using System.Runtime.Versioning;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using NeuroAccess.Nfc;
using NeuroAccessMaui.AndroidPlatform.Nfc;
using NeuroAccessMaui.Services.Nfc;
using Plugin.Firebase.CloudMessaging;

namespace NeuroAccessMaui
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Name = "com.tag.NeuroAccess.MainActivity",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale,
		ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
	[IntentFilter([NfcAdapter.ActionNdefDiscovered],
			Categories = [Intent.CategoryDefault], DataMimeType = "*/*")]
	[IntentFilter([Intent.ActionView],
			Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
			DataSchemes = ["iotid", "iotdisco", "iotsc", "tagsign", "obinfo", "edaler", "nfeat", "xmpp", "aes256", "neuroaccess"])]
	public class MainActivity : MauiAppCompatActivity
	{
		private static NfcAdapter? nfcAdapter = null;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			this.CreateNotificationChannelsIfNeeded();
		}

		private void CreateNotificationChannelsIfNeeded()
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O && OperatingSystem.IsAndroidVersionAtLeast(26))
			{
				this.CreateNotificationChannels();
			}
		}

		[SupportedOSPlatform("android26.0")]
		private void CreateNotificationChannels()
		{
			NotificationChannel MessagesChannel = new("Messages", "Instant Messages", NotificationImportance.High)
			{
				Description = "Channel for incoming Instant Message notifications"
			};

			NotificationChannel PetitionsChannel = new("Petitions", "Petitions sent by other users", NotificationImportance.High)
			{
				Description = "Channel for incoming Contract or Identity Peititions, such as Review or Signature Requests"
			};

			NotificationChannel IdentitiesChannel = new("Identities", "Identity events", NotificationImportance.High)
			{
				Description = "Channel for events relating to the digital identity"
			};

			NotificationChannel ContractsChannel = new("Contracts", "Contract events", NotificationImportance.High)
			{
				Description = "Channel for events relating to smart contracts"
			};

			NotificationChannel EDalerChannel = new("eDaler", "eDaler events", NotificationImportance.High)
			{
				Description = "Channel for events relating to the eDaler wallet balance"
			};

			NotificationChannel TokensChannel = new("Tokens", "Token events", NotificationImportance.High)
			{
				Description = "Channel for events relating to Neuro-Feature tokens"
			};

			NotificationManager? NotificationManager = this.GetSystemService(NotificationService) as NotificationManager;
			NotificationManager?.CreateNotificationChannels(new List<NotificationChannel>()
			{
				MessagesChannel, PetitionsChannel, IdentitiesChannel, ContractsChannel, EDalerChannel, TokensChannel
			});
			FirebaseCloudMessagingImplementation.ChannelId = "Messages"; // Default channel if not specified.

		}

		protected override async void OnPostCreate(Bundle? savedInstanceState)
		{
			try
			{
				base.OnPostCreate(savedInstanceState);
				await this.HandleIntent(this.Intent);
				nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

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

				await App.SendAlertAsync(msg.ToString(), "text/plain");
			}

			App.Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);

		}

		protected override void OnResume()
		{
			base.OnResume();

			if (nfcAdapter is not null)
			{
				Intent Intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);

				PendingIntent? PendingIntent = PendingIntent.GetActivity(this, 0, Intent, PendingIntentFlags.Mutable);
				nfcAdapter.EnableForegroundDispatch(this, PendingIntent, null, null);
			}

			this.RemoveAllNotifications();
		}

		protected override void OnPause()
		{
			base.OnPause();
			nfcAdapter?.DisableForegroundDispatch(this);
		}

		protected override async void OnNewIntent(Intent? Intent)
		{
			base.OnNewIntent(Intent);
			await this.HandleIntent(Intent);
		}

		async Task HandleIntent(Intent? Intent)
		{
			if (Intent is null)
				return;

			try
			{
				switch (Intent.Action)
				{
					case Intent.ActionView:
						string? Url = Intent?.Data?.ToString();
						if (!string.IsNullOrEmpty(Url))
							App.OpenUrlSync(Url);
						break;

					case NfcAdapter.ActionTagDiscovered:
					case NfcAdapter.ActionNdefDiscovered:
					case NfcAdapter.ActionTechDiscovered:
						Tag? Tag = Intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
						if (Tag is null)
							break;

						byte[]? ID = Tag.GetId();
						if (ID is null)
							break;

						string[]? TechList = Tag.GetTechList();
						if (TechList is null)
							break;

						List<INfcInterface> Interfaces = [];

						foreach (string Tech in TechList)
						{
							switch (Tech)
							{
								case "android.nfc.tech.IsoDep":
									IsoDep? IsoDep = IsoDep.Get(Tag);
									if (IsoDep is not null)
										Interfaces.Add(new IsoDepInterface(Tag, IsoDep));
									break;

								case "android.nfc.tech.MifareClassic":
									MifareClassic? MifareClassic = MifareClassic.Get(Tag);
									if (MifareClassic is not null)
										Interfaces.Add(new MifareClassicInterface(Tag, MifareClassic));
									break;

								case "android.nfc.tech.MifareUltralight":
									MifareUltralight? MifareUltralight = MifareUltralight.Get(Tag);
									if (MifareUltralight is not null)
										Interfaces.Add(new MifareUltralightInterface(Tag, MifareUltralight));
									break;

								case "android.nfc.tech.Ndef":
									Ndef? Ndef = Ndef.Get(Tag);
									if (Ndef is not null)
										Interfaces.Add(new NdefInterface(Tag, Ndef));
									break;

								case "android.nfc.tech.NdefFormatable":
									NdefFormatable? NdefFormatable = NdefFormatable.Get(Tag);
									if (NdefFormatable is not null)
										Interfaces.Add(new NdefFormatableInterface(Tag, NdefFormatable));
									break;

								case "android.nfc.tech.NfcA":
									NfcA? NfcA = NfcA.Get(Tag);
									if (NfcA is not null)
										Interfaces.Add(new NfcAInterface(Tag, NfcA));
									break;

								case "android.nfc.tech.NfcB":
									NfcB? NfcB = NfcB.Get(Tag);
									if (NfcB is not null)
										Interfaces.Add(new NfcBInterface(Tag, NfcB));
									break;

								case "android.nfc.tech.NfcBarcode":
									NfcBarcode? NfcBarcode = NfcBarcode.Get(Tag);
									if (NfcBarcode is not null)
										Interfaces.Add(new NfcBarcodeInterface(Tag, NfcBarcode));
									break;

								case "android.nfc.tech.NfcF":
									NfcF? NfcF = NfcF.Get(Tag);
									if (NfcF is not null)
										Interfaces.Add(new NfcFInterface(Tag, NfcF));
									break;

								case "android.nfc.tech.NfcV":
									NfcV? NfcV = NfcV.Get(Tag);
									if (NfcV is not null)
										Interfaces.Add(new NfcVInterface(Tag, NfcV));
									break;
							}
						}

						INfcService Service = App.Instantiate<INfcService>();
						await Service.TagDetected(new NfcTag(ID, [.. Interfaces]));
						break;
				}
			}
			catch (Exception ex)
			{
				Waher.Events.Log.Exception(ex);
				// TODO: Handle read & connection errors.
			}
		}

		private void RemoveAllNotifications()
		{
			NotificationManager? Manager = (NotificationManager?)this.GetSystemService(NotificationService);
			Manager?.CancelAll();
		}
	}
}
