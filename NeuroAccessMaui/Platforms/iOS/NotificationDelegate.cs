using Firebase.CloudMessaging;
using Foundation;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI;
using UIKit;
using UserNotifications;


namespace NeuroAccessMaui
{
	public class NotificationDelegate : UNUserNotificationCenterDelegate
	{

	

		[Export("application:didReceiveRemoteNotification:")]
		public void ReceivedRemoteNotification (UIApplication application, NSDictionary userInfo)
		{
			// this callback will not be fired till the user taps on the notification launching the application.
			// TODO: Handle data of notification

			// Print full message.
			Console.WriteLine ("RECEIVED REMOTE NOTIFICATION");
		}

		[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
		public void DidReceiveRemoteNotification (UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			// If you are receiving a notification message while your app is in the background,
			// TODO: Handle data of notification

			// With swizzling disabled you must let Messaging know about the message, for Analytics, otherwise Firebase won't know if you received the msg
			Messaging.SharedInstance.AppDidReceiveMessage (userInfo);

			// Print full message.
			Console.WriteLine (userInfo);

			completionHandler (UIBackgroundFetchResult.NewData);
		}

	}
}
