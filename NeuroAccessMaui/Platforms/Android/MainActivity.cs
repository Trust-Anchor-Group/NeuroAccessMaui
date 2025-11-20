using System.Runtime.Versioning;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using AndroidX.Core.View;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using NeuroAccess.Nfc;
using NeuroAccessMaui.AndroidPlatform.Nfc;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Intents;
using NeuroAccessMaui.Services.Nfc;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Core.Platforms.Android;

namespace NeuroAccessMaui
{
	[Activity(Exported = true, Theme = "@style/Maui.SplashTheme", MainLauncher = true, Name = "com.tag.NeuroAccess.MainActivity",
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

		protected override async void OnCreate(Bundle? savedInstanceState)
		{
			try
			{
				base.OnCreate(savedInstanceState);
				this.CreateNotificationChannelsIfNeeded();
				nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
				await this.HandleIntent(this.Intent);
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
			if (this.Window is null)
				return;
			WindowCompat.SetDecorFitsSystemWindows(this.Window, false);
			if (OperatingSystem.IsAndroidVersionAtLeast(23) && !OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				// Optionally, set transparent nav/status bars
				this.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
				this.Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
			}
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (nfcAdapter is not null)
			{
				Intent Intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);

				PendingIntentFlags Flags = PendingIntentFlags.UpdateCurrent;
				if (OperatingSystem.IsAndroidVersionAtLeast(31))
				{
					Flags |= PendingIntentFlags.Mutable;
				}

				PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(this, 0, Intent, Flags);
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
			await this.HandleIntent(Intent, false);
		}

		private async Task HandleIntent(Intent? intent, bool Defer = true)
		{
			if (intent is null)
				return;
			try
			{
				AppIntent? AppIntent = null;

				// Retrieve the shared intent service.
				IIntentService IntentService = App.Instantiate<IIntentService>();

				// Handle deep link URL intent.
				if (intent.Action == Intent.ActionView)
				{
					string? Url = intent.Data?.ToString();
					if (!string.IsNullOrEmpty(Url))
					{
						AppIntent = new AppIntent
						{
							Action = Constants.IntentActions.OpenUrl,
							Data = Url
						};
					}
				}
				// Handle NFC intents.
				else if (intent.Action == NfcAdapter.ActionTagDiscovered ||
							intent.Action == NfcAdapter.ActionNdefDiscovered ||
							intent.Action == NfcAdapter.ActionTechDiscovered)
				{
					Tag? Tag = null;
					if (OperatingSystem.IsAndroidVersionAtLeast(33))
						Tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag, Java.Lang.Class.FromType(typeof(Tag))) as Tag;
					else
						Tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
					if (Tag is null)
						return;

					byte[]? Id = Tag.GetId();
					if (Id is null)
						return;

					string[]? TechList = Tag.GetTechList();
					if (TechList is null)
						return;

					List<INfcInterface> Interfaces = new List<INfcInterface>();

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

					// Create a shared NfcTag instance with the extracted data.
					NfcTag NfcTag = new(Id, [.. Interfaces]);
					AppIntent = new()
					{
						Action = Constants.IntentActions.NfcTagDiscovered,
						Payload = NfcTag
					};
				}
				if(AppIntent is null)
					return;

				if (Defer)
					IntentService.QueueIntent(AppIntent);
				else
					await IntentService.ProcessIntentAsync(AppIntent);

			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			return;
		}


		private void RemoveAllNotifications()
		{
			NotificationManager? Manager = (NotificationManager?)this.GetSystemService(NotificationService);
			Manager?.CancelAll();
		}
	}
}
