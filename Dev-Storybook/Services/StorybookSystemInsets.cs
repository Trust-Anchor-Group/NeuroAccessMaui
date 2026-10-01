#if ANDROID
using Android.App;
using Android.Views;
#endif

namespace DevStorybook.Services
{
	/// <summary>
	/// Reads Android system-bar insets for Storybook preview hosts without coupling production views to Storybook.
	/// </summary>
	internal static class StorybookSystemInsets
	{
		/// <summary>
		/// Gets the bottom system-bar inset in device-independent units.
		/// </summary>
		/// <returns>The bottom inset reserved for Android navigation controls.</returns>
		internal static double GetBottomInset()
		{
#if ANDROID
			Activity? CurrentActivity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
			if (CurrentActivity?.Window?.DecorView is not Android.Views.View DecorView)
				return 0.0;

			float Density = CurrentActivity.Resources?.DisplayMetrics?.Density ?? 1.0f;
			WindowInsets? CurrentInsets = DecorView.RootWindowInsets;
			if (CurrentInsets is null)
				return 0.0;

			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				Android.Graphics.Insets SystemBarInsets = CurrentInsets.GetInsets(WindowInsets.Type.SystemBars());
				return SystemBarInsets.Bottom / Density;
			}

			if (OperatingSystem.IsAndroidVersionAtLeast(23))
				return CurrentInsets.SystemWindowInsetBottom / Density;
#endif

			return 0.0;
		}
	}
}
