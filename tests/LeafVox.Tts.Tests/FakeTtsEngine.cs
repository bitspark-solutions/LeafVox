using LeafVox.Tts;

namespace LeafVox.Tts.Tests;

/// <summary>Deterministic instant engine for pipeline tests.</summary>
public sealed class FakeTtsEngine : ITtsEngine
{
    public List<string> Synthesized { get; } = new();
    public List<float> SpeedsSeen { get; } = new();
    public int SampleRate => 1000;
    public bool IsReady => true;
    public string? ActiveVoiceId => "fake";
    public TimeSpan ChunkDuration = TimeSpan.FromMilliseconds(40); // 40 samples @1kHz
    public bool ThrowOnNext;

    public Task LoadAsync(VoiceInfo voice, string modelDir, CancellationToken ct = default) => Task.CompletedTask;
    public void Unload() { }

    public Task<PcmChunk> SynthesizeAsync(string text, float speed, int speakerId, CancellationToken ct = default)
    {
        lock (Synthesized) { Synthesized.Add(text); SpeedsSeen.Add(speed); }
        if (ThrowOnNext) { ThrowOnNext = false; throw new InvalidOperationException("synth boom"); }
        int n = (int)(ChunkDuration.TotalSeconds * SampleRate);
        return Task.FromResult(new PcmChunk(new float[n], SampleRate));
    }
}
