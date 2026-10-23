namespace LeafVox.Tts;

/// <summary>Synthesis seam. Implementations must be safe to call from background threads.</summary>
public interface ITtsEngine
{
    bool IsReady { get; }
    int SampleRate { get; }
    string? ActiveVoiceId { get; }

    Task LoadAsync(VoiceInfo voice, string modelDir, CancellationToken ct = default);
    void Unload();
    Task<PcmChunk> SynthesizeAsync(string text, float speed, int speakerId, CancellationToken ct = default);
}
