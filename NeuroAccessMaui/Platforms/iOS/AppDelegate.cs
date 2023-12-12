using CommunityToolkit.Mvvm.Messaging;
using CoreGraphics;
using Foundation;
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

			return MauiProgram.CreateMauiApp();
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
