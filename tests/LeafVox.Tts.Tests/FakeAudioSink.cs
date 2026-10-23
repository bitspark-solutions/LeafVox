using LeafVox.Tts;

namespace LeafVox.Tts.Tests;

/// <summary>Sink that "plays" instantly and reports position, so pacing never waits in tests.</summary>
public sealed class FakeAudioSink : IAudioSink, IAudioPositionProvider
{
    private double _played;
    public List<PcmChunk> Written { get; } = new();
    public int OpenCount, StopCount, PauseCount, ResumeCount;

    public double PlayedSeconds { get { lock (Written) return _played; } }

    public void Open(int sampleRate)
    {
        Interlocked.Increment(ref OpenCount);
        lock (Written) { _played = 0; Written.Clear(); }
    }
    public void Write(PcmChunk chunk)
    {
        lock (Written)
        {
            Written.Add(chunk);
            _played += chunk.DurationSeconds;
        }
    }
    public void Pause() { Interlocked.Increment(ref PauseCount); }
    public void Resume() { Interlocked.Increment(ref ResumeCount); }
    public void Stop() { Interlocked.Increment(ref StopCount); }
    public void Dispose() { }
}
