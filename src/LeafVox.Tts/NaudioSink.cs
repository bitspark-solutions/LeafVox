using NAudio.Wave;

namespace LeafVox.Tts;

/// <summary>Windows audio output (NAudio DirectSound; resamples internally, so any PCM rate works).</summary>
public sealed class NaudioSink : IAudioSink, IAudioPositionProvider
{
    private CountingProvider? _provider;
    private DirectSoundOut? _out;
    private int _bytesPerSecond;
    private readonly object _gate = new();

    public void Open(int sampleRate)
    {
        lock (_gate)
        {
            CloseInner();
            _bytesPerSecond = sampleRate * sizeof(float); // mono float32
            _provider = new CountingProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1));
            _out = new DirectSoundOut(TimeSpan.FromMilliseconds(150).Milliseconds);
            _out.Init(_provider);
            _out.Play();
        }
    }

    public void Write(PcmChunk chunk)
    {
        lock (_gate)
        {
            if (_provider is null) return;
            var bytes = new byte[chunk.Samples.Length * sizeof(float)];
            Buffer.BlockCopy(chunk.Samples, 0, bytes, 0, bytes.Length);
            _provider.Buffer.AddSamples(bytes, 0, bytes.Length);
        }
    }

    public double PlayedSeconds
    {
        get { lock (_gate) return _provider?.ConsumedSeconds(_bytesPerSecond) ?? 0; }
    }

    public void Pause()
    {
        lock (_gate) _out?.Stop(); // buffer retained; Play() resumes where it stopped
    }

    public void Resume()
    {
        lock (_gate) _out?.Play();
    }

    public void Stop()
    {
        lock (_gate)
        {
            _out?.Stop();
            _provider?.Buffer.ClearBuffer();
            _provider?.ResetCounter();
        }
    }

    private void CloseInner()
    {
        _out?.Dispose();
        _out = null;
        _provider = null;
    }

    public void Dispose()
    {
        lock (_gate) CloseInner();
    }
}
