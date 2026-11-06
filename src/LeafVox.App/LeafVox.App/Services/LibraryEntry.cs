namespace LeafVox.App.Services;

/// <summary>One known book: where the listener stopped, their speed and voice for it.</summary>
public sealed class LibraryEntry
{
    public string Path { get; set; } = "";
    public string Title { get; set; } = "";
    public int LastSentence { get; set; } = -1;
    public double Speed { get; set; } = 1.0;
    public string? VoiceId { get; set; }
    public List<int> Bookmarks { get; set; } = new();
    public DateTime LastOpenedUtc { get; set; } = DateTime.UtcNow;
}
