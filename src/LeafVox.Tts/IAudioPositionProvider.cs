namespace LeafVox.Tts;

/// <summary>Optional sink capability: seconds of audio actually handed to the device (consumed).</summary>
public interface IAudioPositionProvider
{
    double PlayedSeconds { get; }
}
