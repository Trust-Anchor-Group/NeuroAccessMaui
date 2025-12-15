using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Firebase.CloudMessaging;
using Foundation;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
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

			try
			{
				NotificationIntent? intent = this.ParseIntent(userInfo);
				if (intent is not null)
				{
					INotificationServiceV2 service = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
					string raw = JsonSerializer.Serialize(userInfo);
					service.AddAsync(intent, NotificationSource.Push, raw, CancellationToken.None).ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			// Suppress foreground banners; store silently.
			completionHandler(UNNotificationPresentationOptions.None);
        }

        // Called when the user interacts with a notification (e.g., taps it).
        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            var userInfo = response.Notification.Request.Content.UserInfo;
            Console.WriteLine("Notification tapped with data: " + userInfo);
            
			try
			{
				NotificationIntent? intent = this.ParseIntent(userInfo);
				if (intent is not null)
				{
					INotificationServiceV2 service = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
					string raw = JsonSerializer.Serialize(userInfo);
					service.AddAsync(intent, NotificationSource.Push, raw, CancellationToken.None).ConfigureAwait(false);
					string id = service.ComputeId(intent, NotificationSource.Push);
					service.ConsumeAsync(id, CancellationToken.None).ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

            completionHandler();
        }

        public static void ProcessSilentNotification(NSDictionary userInfo)
        {
			try
			{
				NotificationIntent? intent = ParseIntentStatic(userInfo);
				if (intent is null)
					return;

				string raw = JsonSerializer.Serialize(userInfo);
				INotificationServiceV2 service = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				service.AddAsync(intent, NotificationSource.Push, raw, CancellationToken.None).ConfigureAwait(false);

			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
        }

		private NotificationIntent? ParseIntent(NSDictionary userInfo) => ParseIntentStatic(userInfo);

		private static NotificationIntent? ParseIntentStatic(NSDictionary userInfo)
		{
			try
			{
				if (userInfo["notificationIntent"] is NSString intentJson)
				{
					NotificationIntent? parsedNotification = JsonSerializer.Deserialize<NotificationIntent>(intentJson.ToString());
					if (parsedNotification is not null)
						return parsedNotification;
				}

				string? channelId = userInfo["channelId"]?.ToString();
				string? title = userInfo["myTitle"]?.ToString();
				string? body = userInfo["myBody"]?.ToString();

				if (channelId is null)
					return null;

				NotificationIntent intent = new()
				{
					Channel = channelId,
					Title = title ?? string.Empty,
					Body = body
				};

				foreach (NSObject key in userInfo.Keys)
				{
					string k = key.ToString();
					string v = userInfo[key]?.ToString() ?? string.Empty;
					intent.Extras[k] = v;
				}

				if (intent.Extras.TryGetValue("action", out string? action) && Enum.TryParse(action, out NotificationAction parsed))
					intent.Action = parsed;

				if (intent.Extras.TryGetValue("entityId", out string? entityId))
					intent.EntityId = entityId;

				if (intent.Extras.TryGetValue("correlationId", out string? correlationId))
					intent.CorrelationId = correlationId;

				intent.Presentation = ResolvePresentation(intent.Extras);

				return intent;
			}
			catch
			{
				return null;
			}
		}

		private static NotificationPresentation ResolvePresentation(Dictionary<string, string> extras)
		{
			if (extras.TryGetValue("silent", out string silent) && IsTrue(silent))
				return NotificationPresentation.StoreOnly;

			if (extras.TryGetValue("delivery.silent", out string deliverySilent) && IsTrue(deliverySilent))
				return NotificationPresentation.StoreOnly;

			return NotificationPresentation.RenderAndStore;
		}

		private static bool IsTrue(string value)
		{
			return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
				value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
				value.Equals("yes", StringComparison.OrdinalIgnoreCase);
		}
    }
}
