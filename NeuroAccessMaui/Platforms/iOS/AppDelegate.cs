using Foundation;
using NeuroAccessMaui.UI;
using UIKit;


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

			UIWindow? window = null;
			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				window = UIApplication.SharedApplication.ConnectedScenes
					 .OfType<UIWindowScene>()
					 .SelectMany(s => s.Windows)
					 .FirstOrDefault();
			}
			else
				window = UIApplication.SharedApplication.Windows.FirstOrDefault();

			if (window is null)
				return;
			if (!window!.IsKeyWindow)
				window!.MakeKeyWindow();

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
				if (h.PlatformView.CrossPlatformLayout is RadioButton radioButton)
				{
					radioButton.IsEnabled = false;
					radioButton.ControlTemplate = RadioButton.DefaultTemplate;
					radioButton.IsEnabled = true;
					radioButton.ControlTemplate = AppStyles.RadioButtonTemplate;
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
