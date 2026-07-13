using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Microsoft.Maui.ApplicationModel;

namespace ArgoBooks.Mobile;

// Avalonia 12's Android head moved app-builder customization onto the
// Application subclass (see MainApplication.cs); AvaloniaMainActivity is now
// non-generic (it was AvaloniaMainActivity<TApp> in Avalonia 11.x).
[Activity(
    Label = "Argo Books",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
