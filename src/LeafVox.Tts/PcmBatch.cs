namespace LeafVox.Tts;

/// <summary>All audio chunks belonging to one spine sentence.</summary>
public sealed record PcmBatch(int SentenceIndex, IReadOnlyList<PcmChunk> Chunks);
