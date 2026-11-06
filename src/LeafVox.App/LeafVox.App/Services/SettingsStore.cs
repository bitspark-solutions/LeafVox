using System.Text.Json;
using LeafVox.Tts;

namespace LeafVox.App.Services;

/// <summary>App-level settings: active voice + preferred speed. JSON file, zero native deps, works on every OS.</summary>
public static class SettingsStore
{
    private static readonly object Gate = new();
    private static AppSettings? _cached;

    public static AppSettings Load()
    {
        lock (Gate)
        {
            if (_cached is not null) return _cached;
            try
            {
                if (File.Exists(Paths.SettingsFile))
                {
                    _cached = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Paths.SettingsFile)) ?? new AppSettings();
                }
            }
            catch
            {
                _cached = new AppSettings();
            }
            _cached ??= new AppSettings();
            return _cached;
        }
    }

    public static void Save(AppSettings settings)
    {
        lock (Gate)
        {
            _cached = settings;
            try
            {
                File.WriteAllText(Paths.SettingsFile, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (IOException) { /* settings loss is non-fatal */ }
        }
    }
}
