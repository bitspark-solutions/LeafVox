using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace LeafVox.App.Android;

[Activity(
    Label = "LeafVox.App.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
