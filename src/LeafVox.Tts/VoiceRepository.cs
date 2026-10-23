namespace LeafVox.Tts;

/// <summary>Tracks which catalog voices are installed under <see cref="Paths.Models"/> and reads their license notes.</summary>
public sealed class VoiceRepository
{
    private readonly Dictionary<string, string> _installedDirs = new();

    public VoiceRepository() => Refresh();

    /// <summary>Re-scans the models folder and marks each catalog voice installed/partial.</summary>
    public void Refresh()
    {
        _installedDirs.Clear();
        if (!Directory.Exists(Paths.Models)) return;
        foreach (var voice in VoiceCatalog.All)
        {
            var dir = FindModelDirFor(voice);
            if (dir is not null) _installedDirs[voice.Id] = dir;
        }
    }

    public IReadOnlyList<InstalledVoice> List()
    {
        return VoiceCatalog.All.Select(v =>
        {
            var (state, dir) = StateOf(v);
            return new InstalledVoice(v, state, dir, dir is null ? null : ReadLicense(dir));
        }).ToList();
    }

    public InstalledVoice? Get(string id)
    {
        var v = VoiceCatalog.Find(id);
        if (v is null) return null;
        var (state, dir) = StateOf(v);
        return new InstalledVoice(v, state, dir, dir is null ? null : ReadLicense(dir));
    }

    public bool IsInstalled(string id) => _installedDirs.ContainsKey(id);
    public string? ModelDir(string id) => _installedDirs.TryGetValue(id, out var d) ? d : null;

    /// <summary>The preferred voice to auto-load: default if installed, else first installed, else default.</summary>
    public VoiceInfo PreferredVoice()
    {
        if (_installedDirs.ContainsKey(VoiceCatalog.Default.Id)) return VoiceCatalog.Default;
        var firstId = VoiceCatalog.All.FirstOrDefault(v => _installedDirs.ContainsKey(v.Id));
        return firstId ?? VoiceCatalog.Default;
    }

    private (InstallState, string?) StateOf(VoiceInfo v)
    {
        if (_installedDirs.TryGetValue(v.Id, out var dir)) return (InstallState.Installed, dir);
        var partial = Directory.Exists(Paths.Models) &&
            Directory.EnumerateFiles(Paths.Models, "*.part").Any(f => Path.GetFileName(f).Contains(shortened(v)));
        return (partial ? InstallState.Partial : InstallState.NotInstalled, null);
    }

    private static string shortened(VoiceInfo v) =>
        v.ArchiveUrl.Contains("kokoro") ? "kokoro" : v.Id.Length > 6 ? v.Id[..6] : v.Id;

    private string? FindModelDirFor(VoiceInfo v)
    {
        if (!Directory.Exists(Paths.Models)) return null;
        foreach (var dir in Directory.EnumerateDirectories(Paths.Models))
        {
            if (!File.Exists(Path.Combine(dir, "tokens.txt"))) continue;
            var name = Path.GetFileName(dir);
            if (v.ArchiveUrl.Contains(name)) return dir;
        }
        return null;
    }

    private static string? ReadLicense(string modelDir)
    {
        var card = Path.Combine(modelDir, "MODEL_CARD");
        if (File.Exists(card))
        {
            try
            {
                foreach (var line in File.ReadLines(card))
                {
                    var idx = line.IndexOf("License:", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) return line[(idx + "License:".Length)..].Trim();
                }
            }
            catch (IOException) { }
        }
        var lic = Path.Combine(modelDir, "LICENSE");
        if (File.Exists(lic))
        {
            try { return File.ReadLines(lic).FirstOrDefault(l => l.Trim().Length > 0); }
            catch (IOException) { }
        }
        return null;
    }
}
