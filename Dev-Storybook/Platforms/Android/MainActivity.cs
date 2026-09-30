using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace DevStorybook
{
	/// <summary>
	/// Provides the Android activity for the developer Storybook.
	/// </summary>
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
		ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale,
		ScreenOrientation = ScreenOrientation.Portrait)]
	public class MainActivity : MauiAppCompatActivity
	{
		/// <inheritdoc/>
		protected override void OnCreate(Bundle? SavedInstanceState)
		{
			base.OnCreate(SavedInstanceState);
			if (this.Window is null)
				return;

			WindowCompat.SetDecorFitsSystemWindows(this.Window, false);
			if (OperatingSystem.IsAndroidVersionAtLeast(23) && !OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				this.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
				this.Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
			}
		}
	}
}
