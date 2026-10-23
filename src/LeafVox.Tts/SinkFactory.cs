namespace LeafVox.Tts;

/// <summary>Picks the best available playback sink for the current OS.</summary>
public static class SinkFactory
{
    public static IAudioSink CreateDefault() =>
        OperatingSystem.IsWindows() ? new NaudioSink() : new MiniAudioSink();
}
