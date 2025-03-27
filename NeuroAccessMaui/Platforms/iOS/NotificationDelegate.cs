using System;
using Firebase.CloudMessaging;
using Foundation;
using UIKit;
using UserNotifications;

namespace NeuroAccessMaui
{
    public class NotificationDelegate : UNUserNotificationCenterDelegate
    {
         // Called when a notification is delivered while the app is in the foreground.
        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            var userInfo = notification.Request.Content.UserInfo;
            
			Messaging.SharedInstance.AppDidReceiveMessage(userInfo);

            Console.WriteLine("Foreground notification received: " + userInfo);

            // Choose to show alert and play sound even when app is foregrounded.
            completionHandler(UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Sound);
        }

        // Called when the user interacts with a notification (e.g., taps it).
        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            var userInfo = response.Notification.Request.Content.UserInfo;
            Console.WriteLine("Notification tapped with data: " + userInfo);
            
            // Handle the tap (e.g., navigate to a specific page in your app).
            
            completionHandler();
        }

        public static void ProccessSilentNotification(NSDictionary userInfo)
                {
            Console.WriteLine("Silen notification received: " + userInfo);
            
            // Handle the tap (e.g., navigate to a specific page in your app).
        }
    }
}
