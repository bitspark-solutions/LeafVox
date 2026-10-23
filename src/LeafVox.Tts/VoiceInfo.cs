namespace LeafVox.Tts;

/// <summary>A free downloadable voice offered by the catalog or installed locally.</summary>
public sealed record VoiceInfo(
    string Id,
    string DisplayName,
    string Language,
    VoiceTier Tier,
    ModelKind Kind,
    string ArchiveUrl,
    long ArchiveBytes,
    int SpeakerCount = 1)
{
    /// <summary>Name of the extracted folder inside the archive (usually equals the archive stem).</summary>
    public string FolderName => System.IO.Path.GetFileNameWithoutExtension(
        ArchiveUrl.EndsWith(".bz2") ? System.IO.Path.GetFileNameWithoutExtension(ArchiveUrl) : System.IO.Path.GetFileName(ArchiveUrl));
}
