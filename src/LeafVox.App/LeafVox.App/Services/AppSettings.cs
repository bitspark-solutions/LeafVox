namespace LeafVox.App.Services;

/// <summary>Persisted user preferences.</summary>
public sealed class AppSettings
{
    public string? VoiceId { get; set; }
    public double Speed { get; set; } = 1.0;
    public int SpeakerId { get; set; }
}
