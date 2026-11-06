using System.Text.Json;
using LeafVox.Tts;

namespace LeafVox.App.Services;

/// <summary>Recent documents + resume positions, stored as JSON under the app data dir.</summary>
public sealed class LibraryStore
{
    private readonly object _gate = new();
    private readonly List<LibraryEntry> _entries;
    private readonly string _file;

    public LibraryStore() : this(Path.Combine(Paths.Data, "library.json")) { }

    public LibraryStore(string file)
    {
        _file = file;
        _entries = Load(file);
    }

    public IReadOnlyList<LibraryEntry> Entries
    {
        get { lock (_gate) return _entries.OrderByDescending(e => e.LastOpenedUtc).ToList(); }
    }

    public LibraryEntry Touch(string path, string title)
    {
        lock (_gate)
        {
            var e = _entries.FirstOrDefault(x => x.Path == path) ?? new LibraryEntry();
            e.Path = path;
            e.Title = title;
            e.LastOpenedUtc = DateTime.UtcNow;
            _entries.Remove(e);
            _entries.Add(e);
            SaveInner();
            return e;
        }
    }

    public void SavePosition(string path, int lastSentence, double speed)
    {
        lock (_gate)
        {
            var e = _entries.FirstOrDefault(x => x.Path == path);
            if (e is null) return;
            e.LastSentence = lastSentence;
            e.Speed = speed;
            SaveInner();
        }
    }

    public void ToggleBookmark(string path, int sentenceIndex)
    {
        lock (_gate)
        {
            var e = _entries.FirstOrDefault(x => x.Path == path);
            if (e is null) return;
            if (e.Bookmarks.Contains(sentenceIndex)) e.Bookmarks.Remove(sentenceIndex);
            else e.Bookmarks.Add(sentenceIndex);
            e.Bookmarks.Sort();
            SaveInner();
        }
    }

    private static List<LibraryEntry> Load(string file)
    {
        try
        {
            return File.Exists(file)
                ? JsonSerializer.Deserialize<List<LibraryEntry>>(File.ReadAllText(file)) ?? new()
                : new();
        }
        catch
        {
            return new();
        }
    }

    private void SaveInner()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            File.WriteAllText(_file, JsonSerializer.Serialize(_entries));
        }
        catch (IOException) { /* library loss is non-fatal */ }
    }
}
