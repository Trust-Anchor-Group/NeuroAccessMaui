using System;
using Firebase.CloudMessaging;
using Foundation;
using NeuroAccessMaui.Services;
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

        public static void ProcessSilentNotification(NSDictionary userInfo)
        {
            // Extract values from the NSDictionary.
            string? Title = userInfo["myTitle"]?.ToString();
            string? Body = userInfo["myBody"]?.ToString();
            string? ChannelId = userInfo["channelId"]?.ToString();

            if(Title == null || Body == null)
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
        }
    }
}
