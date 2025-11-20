using Firebase.CloudMessaging;
using Foundation;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Intents;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI;
using Plugin.Firebase.Core.Platforms.iOS;
using UIKit;
using UserNotifications;


namespace NeuroAccessMaui
{
	[Register("AppDelegate")]
	public class AppDelegate : MauiUIApplicationDelegate
	{

		protected override MauiApp CreateMauiApp()
		{
			MauiApp app = MauiProgram.CreateMauiApp();
#warning Remove this line when the issue with the RadioButton control template is fixed in a future version of MAUI https://github.com/dotnet/maui/issues/19478

			// TODO: This is a temporary workaround to fix the issue with custom RadioButton control templates not responding to taps on IOS
			// https://github.com/dotnet/maui/issues/19478
			RadioButtonTemplateWorkaround();

			return app;
		}

		[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
		public static void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			Console.WriteLine("Silent data notification received: " + userInfo);

			// Extract values from the NSDictionary.
			string? Title = userInfo["myTitle"]?.ToString();
			string? Body = userInfo["myBody"]?.ToString();
			string? ChannelId = userInfo["channelId"]?.ToString();

			if (Title is null || Body is null)
			{
				ServiceRef.LogService.LogWarning("NotificationDelegate, Received notification with missing title or body.");
				return;
			}

			// Convert the NSDictionary to an IDictionary<string, string>
			Dictionary<string, string> Payload = new Dictionary<string, string>();
			foreach (NSObject Key in userInfo.Keys)
			{
				Payload[Key.ToString()] = userInfo[Key]?.ToString() ?? string.Empty;
			}


			// Switch based on channelId to handle different types of notifications.
			switch (ChannelId)
			{
				case Constants.PushChannels.Messages:
					ServiceRef.PlatformSpecific.ShowMessageNotification(Title, Body, Payload);
					break;

				case Constants.PushChannels.Petitions:
					ServiceRef.PlatformSpecific.ShowPetitionNotification(Title, Body, Payload);
					break;

				case Constants.PushChannels.Identities:
					ServiceRef.PlatformSpecific.ShowIdentitiesNotification(Title, Body, Payload);
					break;

				case Constants.PushChannels.Contracts:
					ServiceRef.PlatformSpecific.ShowContractsNotification(Title, Body, Payload);
					break;

				case Constants.PushChannels.EDaler:
					ServiceRef.PlatformSpecific.ShowEDalerNotification(Title, Body, Payload);
					break;

				case Constants.PushChannels.Tokens:
					ServiceRef.PlatformSpecific.ShowTokenNotification(Title, Body, Payload);
					break;

				case Constants.PushChannels.Provisioning:
					ServiceRef.PlatformSpecific.ShowProvisioningNotification(Title, Body, Payload);
					break;

				default:
					// ignore
					break;
			}

			completionHandler(UIBackgroundFetchResult.NewData);
		}

		[Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
		public static void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
		{
			Messaging.SharedInstance.ApnsToken = deviceToken;
		}

		/// <summary>
		/// Method is called when an URL with a registered schema is being opened.
		/// </summary>
		/// <param name="app">Application</param>
		/// <param name="url">URL</param>
		/// <param name="options">Options</param>
		/// <returns>If URL is handled.</returns>
		public override bool OpenUrl(UIApplication Application, NSUrl Url, NSDictionary Options)
		{
			if (string.IsNullOrEmpty(Url.AbsoluteString))
				return false;

			try
			{
				// Create a deep link intent.
				AppIntent AppIntent = new()
				{
					Action = Constants.IntentActions.OpenUrl,
					Data = Url.AbsoluteString
				};

				// Retrieve the shared intent service and queue the intent.
				IIntentService IntentService = App.Instantiate<IIntentService>();
				if (ServiceRef.TagProfile.Step == RegistrationStep.GetStarted)
					IntentService.ProcessIntentAsync(AppIntent).ConfigureAwait(false);
				else
					IntentService.QueueIntent(AppIntent);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return false;
			}
			return true;
		}


		// At the time of writing 8/4/2024
		// There is a bug with the MAUI/Mopups library that causes the app to crash because some of the apps lifecycle events are not forwarded properly
		// https://github.com/LuckyDucko/Mopups/issues/95
		// https://github.com/dotnet/maui/issues/20408
		// Might not be needed in future versions of MAUI/Mopups
		// By ensuring we have a KeyWindow when the app is activated or resigns solves this issue.
		public override void OnActivated(UIApplication application)
		{
			EnsureKeyWindow();
			base.OnActivated(application);
		}

		public override void OnResignActivation(UIApplication application)
		{
			EnsureKeyWindow();
			base.OnResignActivation(application);
		}

		private static void EnsureKeyWindow()
		{
			if (GetKeyWindow() is null)
				return;

			UIWindow? Window = null;
			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				Window = UIApplication.SharedApplication.ConnectedScenes
					 .OfType<UIWindowScene>()
					 .SelectMany(s => s.Windows)
					 .FirstOrDefault();
			}
			else
				Window = UIApplication.SharedApplication.Windows.FirstOrDefault();

			if (Window is null)
				return;
			if (!Window!.IsKeyWindow)
				Window!.MakeKeyWindow();

		}

		/// <summary>
		/// Gets the Key Window for the application.
		/// IOS version specific
		/// </summary>
		internal static UIWindow? GetKeyWindow()
		{
			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
			{
				return UIApplication.SharedApplication.ConnectedScenes
					.OfType<UIWindowScene>()
					.SelectMany(s => s.Windows)
					.FirstOrDefault(w => w.IsKeyWindow);
			}
			else if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
				return UIApplication.SharedApplication.Windows.FirstOrDefault(w => w.IsKeyWindow);
			else
				return UIApplication.SharedApplication.KeyWindow;
		}

		private static void RadioButtonTemplateWorkaround()
		{
			Microsoft.Maui.Handlers.RadioButtonHandler.Mapper.AppendToMapping("TemplateWorkaround", (h, v) =>
			{
				if (h.PlatformView.CrossPlatformLayout is RadioButton RadioButton)
				{
					RadioButton.IsEnabled = false;
					RadioButton.ControlTemplate = RadioButton.DefaultTemplate;
					RadioButton.IsEnabled = true;
					RadioButton.ControlTemplate = AppStyles.RadioButtonTemplate;
				}
			});
		}



		/*
		Not needed anymore as we have a new way to handle keyboard events in PlatformSpecific.cs, keeping this until new implementation has been tested
				private void RegisterKeyBoardObserver()
				{
					this.onKeyboardShowObserver ??= UIKeyboard.Notifications.ObserveWillShow((object? Sender, UIKeyboardEventArgs Args) =>
					{
						NSDictionary? UserInfo = Args.Notification.UserInfo;

						if (UserInfo is null)
						{
							return;
						}

						NSValue Result = (NSValue)UserInfo.ObjectForKey(new NSString(UIKeyboard.FrameEndUserInfoKey));
						CGSize keyboardSize = Result.RectangleFValue.Size;

						WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage((float)keyboardSize.Height));
					});

					this.onKeyboardHideObserver ??= UIKeyboard.Notifications.ObserveWillHide((object? Sender, UIKeyboardEventArgs Args) =>
					{
						WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(0));
					});
				}
		*/
	}
}
