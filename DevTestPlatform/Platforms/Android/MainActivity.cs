using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace DevTestPlatform;

/// <summary>Hosts current MAUI production previews in the developer Android application.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
    ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale)]
public sealed class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc/>
    protected override void OnCreate(Bundle? SavedInstanceState)
    {
        base.OnCreate(SavedInstanceState);
        if (this.Window is not null)
        {
            WindowCompat.SetDecorFitsSystemWindows(this.Window, false);
            this.Window.SetSoftInputMode(SoftInput.AdjustResize);
        }
    }
}
