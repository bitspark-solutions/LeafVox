namespace LeafVox.Tts;

/// <summary>App data locations: %LOCALAPPDATA%/leafvox on Windows, ~/.local/share/leafvox elsewhere.</summary>
public static class Paths
{
    public static string Root
    {
        get
        {
            var baseDir = OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.local/share";
            var dir = Path.Combine(baseDir, "leafvox");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string Models => Path.Combine(Root, "models");
    public static string Data => Path.Combine(Root, "data");
    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string DatabaseFile => Path.Combine(Data, "library.db");
}
