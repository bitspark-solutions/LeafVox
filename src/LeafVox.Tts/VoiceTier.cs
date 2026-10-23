namespace LeafVox.Tts;

/// <summary>Quality/size trade-off of a voice model.</summary>
public enum VoiceTier
{
    /// <summary>Small VITS/Mimic3 model, very fast on CPU.</summary>
    Light,
    /// <summary>Large natural model (Kokoro), best free quality.</summary>
    Natural,
}
