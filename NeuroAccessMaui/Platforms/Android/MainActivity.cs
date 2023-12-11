using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;

namespace NeuroAccessMaui
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale,
		ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
	[IntentFilter([NfcAdapter.ActionNdefDiscovered],
			Categories = [Intent.CategoryDefault], DataMimeType = "*/*")]
	[IntentFilter([Intent.ActionView],
			Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
			DataSchemes = ["iotid", "tagsign", "obinfo"])]
	public class MainActivity : MauiAppCompatActivity
	{
	}
}
