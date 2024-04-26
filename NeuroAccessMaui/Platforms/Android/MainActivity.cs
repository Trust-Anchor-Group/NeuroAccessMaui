using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;

namespace NeuroAccessMaui
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale,
		ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
	[IntentFilter([NfcAdapter.ActionNdefDiscovered],
			Categories = [Intent.CategoryDefault], DataMimeType = "*/*")]
	[IntentFilter([Intent.ActionView],
			Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
			DataSchemes = ["iotid", "iotdisco", "iotsc", "tagsign", "obinfo", "edaler", "nfeat", "xmpp", "aes256", "neuroaccess"])]
	public class MainActivity : MauiAppCompatActivity
	{

		protected override void OnPostCreate(Bundle? savedInstanceState)
		{
			try
			{
				base.OnPostCreate(savedInstanceState);
			}
			catch (Exception ex)
			{
					StringBuilder msg = new();

					msg.AppendLine("An error occurred in the Android MainActivity.OnPostCreate method.");
					msg.AppendLine("Exception message:");
					msg.Append(ex.Message);
					msg.AppendLine();
					msg.AppendLine("```");
					msg.AppendLine(ex.StackTrace);
					msg.AppendLine("```");

					App.SendAlert(msg.ToString(), "text/plain").Wait();
			}
		}
	}
}
