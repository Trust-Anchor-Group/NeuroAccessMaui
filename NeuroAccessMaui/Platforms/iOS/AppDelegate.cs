using CommunityToolkit.Mvvm.Messaging;
using CoreGraphics;
using Foundation;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI;
using UIKit;
using Waher.Runtime.Inventory;

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

			#warning Try removing this when the Mopups package is updated to 1.3.1
			// TODO: This is a temporary workaround to fix the issue with the first popup causing a crash when sleeping the app
			// This is a known issue with the Mopup library and might be fixed in 1.3.1
			MopupService.Instance.Popped += ActivateWindowOnPopupPop;

			return app;
		}

		private static void ActivateWindowOnPopupPop(object? Sender, EventArgs e)
		{
			Current.OnActivated(UIApplication.SharedApplication);
			MopupService.Instance.Popped -= ActivateWindowOnPopupPop;
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
