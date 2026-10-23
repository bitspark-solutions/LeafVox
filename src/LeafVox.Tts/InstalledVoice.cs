namespace LeafVox.Tts;

/// <summary>A catalog voice plus its local status.</summary>
public sealed record InstalledVoice(VoiceInfo Voice, InstallState State, string? Directory, string? LicenseNote)
{
    public bool IsUsable => State == InstallState.Installed && Directory is not null;
}
