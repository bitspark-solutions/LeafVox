using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Miniaudio;

namespace LeafVox.Tts;

/// <summary>
/// Cross-platform audio output over miniaudio (win/linux/mac natives ship in the NuGet package).
/// Windows normally uses <see cref="NaudioSink"/>; this sink is the path for macOS/Linux —
/// and it also runs on Windows, which is how it gets real-device verification from the spike suite.
/// </summary>
public sealed unsafe class MiniAudioSink : IAudioSink, IAudioPositionProvider
{
    private static readonly ConcurrentDictionary<nint, MiniAudioSink> Instances = new();

    private ma_context* _ctx;
    private ma_device* _dev;
    private readonly Queue<float[]> _pending = new();
    private float[] _reservoir = Array.Empty<float>();
    private int _resPos;
    private long _deliveredFrames;
    private int _sampleRate;
    private volatile bool _paused;
    private readonly object _gate = new();

    public void Open(int sampleRate)
    {
        CloseInner();
        _sampleRate = sampleRate;
        _ctx = (ma_context*)NativeMemory.AllocZeroed(1, (nuint)sizeof(ma_context));
        _dev = (ma_device*)NativeMemory.AllocZeroed(1, (nuint)sizeof(ma_device));

        var ctxErr = ma.context_init(null, 0, null, _ctx);
        if (ctxErr != ma_result.MA_SUCCESS)
            throw new LeafVoxTtsException($"miniaudio context init failed ({ctxErr}).");

        var config = ma.device_config_init(ma_device_type.ma_device_type_playback);
        config.sampleRate = (uint)sampleRate;
        config.playback.format = ma_format.ma_format_f32;
        config.playback.channels = 1;
        config.dataCallback = (delegate* unmanaged[Cdecl]<ma_device*, void*, void*, uint, void>)&OnData;

        var devErr = ma.device_init(_ctx, &config, _dev);
        if (devErr != ma_result.MA_SUCCESS)
            throw new LeafVoxTtsException($"miniaudio playback device init failed ({devErr}).");

        Instances[(nint)_dev] = this;
        ma.device_start(_dev);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnData(ma_device* device, void* output, void* input, uint frameCount)
    {
        if (Instances.TryGetValue((nint)device, out var sink))
            sink.Fill((float*)output, (int)frameCount);
    }

    private void Fill(float* output, int frames)
    {
        var outSpan = new Span<float>(output, frames);
        outSpan.Clear();
        if (_paused) return; // clock freezes while paused

        int written = 0;
        lock (_gate)
        {
            while (written < frames)
            {
                if (_resPos >= _reservoir.Length)
                {
                    if (_pending.Count == 0) break;
                    _reservoir = _pending.Dequeue();
                    _resPos = 0;
                    continue;
                }
                int take = Math.Min(frames - written, _reservoir.Length - _resPos);
                _reservoir.AsSpan(_resPos, take).CopyTo(outSpan[written..]);
                _resPos += take;
                written += take;
            }
            Interlocked.Add(ref _deliveredFrames, written);
        }
    }

    public void Write(PcmChunk chunk)
    {
        lock (_gate) _pending.Enqueue(chunk.Samples);
    }

    public double PlayedSeconds => Volatile.Read(ref _deliveredFrames) / (double)Math.Max(1, _sampleRate);

    public void Pause() => _paused = true;

    public void Resume() => _paused = false;

    public void Stop()
    {
        lock (_gate)
        {
            _pending.Clear();
            _reservoir = Array.Empty<float>();
            _resPos = 0;
            Interlocked.Exchange(ref _deliveredFrames, 0);
        }
        _paused = false;
    }

    private void CloseInner()
    {
        if (_dev is not null)
        {
            Instances.TryRemove((nint)_dev, out _);
            ma.device_uninit(_dev);
            NativeMemory.Free(_dev);
            _dev = null;
        }
        if (_ctx is not null)
        {
            ma.context_uninit(_ctx);
            NativeMemory.Free(_ctx);
            _ctx = null;
        }
    }

    public void Dispose() => CloseInner();
}
