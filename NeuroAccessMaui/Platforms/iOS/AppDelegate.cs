using CommunityToolkit.Mvvm.Messaging;
using CoreGraphics;
using Foundation;
using NeuroAccessMaui.UI;
using UIKit;


namespace NeuroAccessMaui
{
	[Register("AppDelegate")]
	public class AppDelegate : MauiUIApplicationDelegate
	{
		private NSObject? onKeyboardShowObserver;
		private NSObject? onKeyboardHideObserver;

		protected override MauiApp CreateMauiApp()
		{
			this.RegisterKeyBoardObserver();
			MauiApp app = MauiProgram.CreateMauiApp();
			#warning Remove this line when the issue with the RadioButton control template is fixed in a future version of MAUI https://github.com/dotnet/maui/issues/19478

			// TODO: This is a temporary workaround to fix the issue with RadioButton control template making the buttons unclickable on IOS
			// https://github.com/dotnet/maui/issues/19478
			this.RadioButtonTemplateWorkaround();

			return app;
		}

		// At the time of writing 8/4/2024
		// There is a bug with the MAUI/Mopups library that causes the app to crash because the windows lifecycle events are not properly managed.
		// https://github.com/LuckyDucko/Mopups/issues/95
		// https://github.com/dotnet/maui/issues/20408
		// Here we handle those events manually to prevent the app from crashing.
		// Might not be needed in future versions of MAUI/Mopups
		// By ensuring we have a KeyWindow when the app is activated or resigns solves this issue.
		public override void OnActivated(UIApplication application)
		{
			this.EnsureKeyWindow();
			base.OnActivated(application);
		}

		public override void OnResignActivation(UIApplication application)
		{
			this.EnsureKeyWindow();
			base.OnResignActivation(application);
		}

		private void EnsureKeyWindow()
		{
			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				UIApplication.SharedApplication.ConnectedScenes
					 .OfType<UIWindowScene>()
					 .SelectMany(s => s.Windows)
					 .FirstOrDefault()?.MakeKeyWindow();
			}
			else
				UIApplication.SharedApplication.Windows.FirstOrDefault()?.MakeKeyWindow();
		}
		private void RadioButtonTemplateWorkaround()
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
	}
}
