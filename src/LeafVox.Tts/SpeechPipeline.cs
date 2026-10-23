using System.Threading.Channels;
using LeafVox.Core;

namespace LeafVox.Tts;

/// <summary>
/// Turns a <see cref="ReadingSpine"/> into spoken audio: one producer task synthesizes
/// sentence batches ahead of playback (bounded prefetch), one consumer task feeds the sink
/// and raises sentence events the UI highlights against.
/// </summary>
public sealed class SpeechPipeline : IAsyncDisposable
{
    private const int PrefetchDepth = 2;

    private readonly ITtsEngine _engine;
    private readonly IAudioSink _sink;
    private readonly object _gate = new();

    private ReadingSpine? _spine;
    private CancellationTokenSource? _epoch;
    private Task? _producer;
    private Task? _consumer;
    private volatile int _index;
    private volatile float _speed = 1.0f;
    private volatile int _speakerId;
    private volatile SpeechState _state = SpeechState.Stopped;

    public event EventHandler<int>? SentenceStarted;
    public event EventHandler<int>? SentenceFinished;
    public event EventHandler? Finished;
    public event EventHandler<Exception>? Failed;

    public SpeechPipeline(ITtsEngine engine, IAudioSink sink)
    {
        _engine = engine;
        _sink = sink;
    }

    public SpeechState State => _state;
    public int CurrentIndex => _index;
    public float Speed { get => _speed; set => _speed = Math.Clamp(value, 0.5f, 3.0f); }
    public int SpeakerId { get => _speakerId; set => _speakerId = value; }

    public void SetSpine(ReadingSpine spine)
    {
        Stop();
        _spine = spine;
    }

    /// <summary>Play from the given sentence (or current position when negative).</summary>
    public void Play(int fromIndex = -1)
    {
        var spine = _spine;
        if (spine is null || spine.Sentences.Count == 0) return;

        lock (_gate)
        {
            _epoch?.Cancel();
            var cts = new CancellationTokenSource();
            _epoch = cts;
            var token = cts.Token;
            _sink.Stop();
            _sink.Open(_engine.SampleRate > 0 ? _engine.SampleRate : 22050);
            _index = fromIndex >= 0 ? spine.ClampIndex(fromIndex) : spine.ClampIndex(_index);
            _state = SpeechState.Playing;

            var queue = Channel.CreateBounded<PcmBatch>(new BoundedChannelOptions(PrefetchDepth)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
            });
            _producer = Task.Run(() => ProduceAsync(spine, _index, queue.Writer, token));
            _consumer = Task.Run(() => ConsumeAsync(spine, queue.Reader, token));
        }
    }

    public void Pause()
    {
        if (_state != SpeechState.Playing) return;
        _state = SpeechState.Paused;
        _sink.Pause();
    }

    public void Resume()
    {
        if (_state != SpeechState.Paused) return;
        _state = SpeechState.Playing;
        _sink.Resume();
    }

    public void Toggle()
    {
        switch (_state)
        {
            case SpeechState.Playing: Pause(); break;
            case SpeechState.Paused: Resume(); break;
            default: Play(); break;
        }
    }

    public void Next() { var s = _spine; if (s is not null) Play(Math.Min(_index + 1, s.Sentences.Count - 1)); }
    public void Previous() { var s = _spine; if (s is not null) Play(Math.Max(_index - 1, 0)); }
    public void Seek(int sentenceIndex) { var s = _spine; if (s is not null) Play(s.ClampIndex(sentenceIndex)); }

    /// <summary>Jump without starting playback (for bookmark restore).</summary>
    public void PositionOnly(int sentenceIndex)
    {
        var s = _spine;
        if (s is not null) _index = s.ClampIndex(sentenceIndex);
    }

    public void Stop()
    {
        lock (_gate)
        {
            CancelInner();
            _state = SpeechState.Stopped;
        }
    }

    private void CancelInner()
    {
        try { _epoch?.Cancel(); } catch { /* ignore */ }
        _sink.Stop();
    }

    private async Task ProduceAsync(ReadingSpine spine, int start, ChannelWriter<PcmBatch> writer, CancellationToken token)
    {
        try
        {
            for (int i = start; i < spine.Sentences.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var pieces = ChunkSplitter.Split(spine.Sentences[i].Text);
                var chunks = new List<PcmChunk>(pieces.Count);
                foreach (var piece in pieces)
                {
                    chunks.Add(await _engine.SynthesizeAsync(piece, _speed, _speakerId, token).ConfigureAwait(false));
                    // Respect pause: don't run synthesis far ahead while paused.
                    while (_state == SpeechState.Paused && !token.IsCancellationRequested)
                        await Task.Delay(40, token).ConfigureAwait(false);
                }
                await writer.WriteAsync(new PcmBatch(i, chunks), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* expected on stop/seek */ }
        catch (Exception ex)
        {
            try { writer.TryComplete(ex); } catch { }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task ConsumeAsync(ReadingSpine spine, ChannelReader<PcmBatch> reader, CancellationToken token)
    {
        bool naturalEnd = false;
        var pos = _sink as IAudioPositionProvider;
        double boundary = 0; // seconds of audio scheduled before the current batch
        try
        {
            await foreach (var batch in reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                double duration = batch.Chunks.Sum(c => c.DurationSeconds);
                if (pos is not null)
                    await WaitUntilPlayedAsync(pos, boundary, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                _index = batch.SentenceIndex;
                RaiseOnUi(SentenceStarted, batch.SentenceIndex);
                foreach (var chunk in batch.Chunks)
                {
                    _sink.Write(chunk);
                    token.ThrowIfCancellationRequested();
                }
                boundary += duration;
                naturalEnd = batch.SentenceIndex == spine.Sentences.Count - 1;
                RaiseOnUi(SentenceFinished, batch.SentenceIndex);
            }
        }
        catch (OperationCanceledException) { }
        catch (ChannelClosedException ex) when (ex.InnerException is not null)
        {
            RaiseOnUi(Failed, ex.InnerException);
        }
        catch (Exception ex)
        {
            RaiseOnUi(Failed, ex);
        }
        finally
        {
            // Only flip to Stopped if this epoch is still the live one.
            bool live = !token.IsCancellationRequested;
            if (live)
            {
                _state = SpeechState.Stopped;
                if (naturalEnd) Finished?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private static async Task WaitUntilPlayedAsync(IAudioPositionProvider pos, double target, CancellationToken token)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30); // safety: never hang the pipeline
        while (pos.PlayedSeconds < target - 0.03 && !token.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25, token).ConfigureAwait(false);
        }
    }

    private void RaiseOnUi<T>(EventHandler<T>? handler, T arg)
        => handler?.Invoke(this, arg);

    public async ValueTask DisposeAsync()
    {
        Stop();
        if (_producer is not null) try { await _producer.ConfigureAwait(false); } catch { }
        if (_consumer is not null) try { await _consumer.ConfigureAwait(false); } catch { }
        _sink.Dispose();
        (_engine as IDisposable)?.Dispose();
    }
}
