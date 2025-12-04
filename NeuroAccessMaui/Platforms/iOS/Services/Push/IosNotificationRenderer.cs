using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using NeuroAccessMaui.Services.Notification;
using UserNotifications;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Renders local notifications on iOS.
	/// </summary>
	public sealed class IosNotificationRenderer : INotificationRenderer
	{
		/// <summary>
		/// Displays a local notification with the provided intent.
		/// </summary>
		/// <param name="Intent">Notification intent to render.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public async Task RenderAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			if (Intent.Presentation is NotificationPresentation.StoreOnly or NotificationPresentation.Transient)
				return;

			UNMutableNotificationContent content = new()
			{
				Title = Intent.Title,
				Body = Intent.Body ?? string.Empty,
				Sound = UNNotificationSound.Default,
				UserInfo = this.BuildUserInfo(Intent)
			};

			UNNotificationRequest request = UNNotificationRequest.FromIdentifier(
				NSUuid.NewUuid().AsString(),
				content,
				trigger: null);

			await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
		}

		private NSDictionary BuildUserInfo(NotificationIntent Intent)
		{
			NSMutableDictionary dictionary = new();

			if (!string.IsNullOrEmpty(Intent.Channel))
				dictionary.SetValueForKey(new NSString(Intent.Channel), new NSString("channelId"));

			dictionary.SetValueForKey(new NSString(Intent.Title), new NSString("myTitle"));

			if (!string.IsNullOrEmpty(Intent.Body))
				dictionary.SetValueForKey(new NSString(Intent.Body), new NSString("myBody"));

			dictionary.SetValueForKey(new NSString(Intent.Action.ToString()), new NSString("action"));

			if (!string.IsNullOrEmpty(Intent.EntityId))
				dictionary.SetValueForKey(new NSString(Intent.EntityId), new NSString("entityId"));

			if (!string.IsNullOrEmpty(Intent.CorrelationId))
				dictionary.SetValueForKey(new NSString(Intent.CorrelationId), new NSString("correlationId"));

			foreach (KeyValuePair<string, string> pair in Intent.Extras)
			{
				dictionary.SetValueForKey(new NSString(pair.Value), new NSString(pair.Key));
			}

			try
			{
				string json = JsonSerializer.Serialize(Intent);
				dictionary.SetValueForKey(new NSString(json), new NSString("notificationIntent"));
			}
			catch
			{
				// If serialization fails, continue without the aggregated payload.
			}

			return dictionary;
		}
	}
}
