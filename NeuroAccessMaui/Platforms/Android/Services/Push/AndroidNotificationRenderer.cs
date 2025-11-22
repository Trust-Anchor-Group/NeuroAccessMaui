using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidNotification = Android.App.Notification;
using NeuroAccessMaui.Services.Notification;
using System.Text.Json;
using System;
using NeuroAccessMaui;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Renders local notifications on Android.
	/// </summary>
	public sealed class AndroidNotificationRenderer : INotificationRenderer
	{
		private const string DefaultChannelId = "default";
		private readonly HashSet<string> createdChannels = new();

		/// <summary>
		/// Displays a local notification with the provided title and message.
		/// </summary>
		/// <param name="Title">Notification title.</param>
		/// <param name="Message">Notification body.</param>
		/// <param name="Channel">Notification channel identifier.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public Task RenderAsync(NotificationIntent notificationIntent, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string channelId = string.IsNullOrWhiteSpace(notificationIntent.Channel) ? DefaultChannelId : notificationIntent.Channel;

			NotificationManager? manager = Android.App.Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
			if (manager is null)
				return Task.CompletedTask;

			this.EnsureChannel(manager, channelId);

			int iconId = Android.App.Application.Context?.ApplicationInfo?.Icon ?? Android.Resource.Drawable.SymDefAppIcon;

			NotificationCompat.Builder builder = new NotificationCompat.Builder(Android.App.Application.Context, channelId)
				.SetContentTitle(notificationIntent.Title)
				.SetContentText(notificationIntent.Body ?? string.Empty)
				.SetSmallIcon(iconId)
				.SetAutoCancel(true)
				.SetPriority((int)NotificationPriority.High);

			if (OperatingSystem.IsAndroidVersionAtLeast(26))
			{
				builder.SetCategory(AndroidNotification.CategoryMessage);
			}

			try
			{
				string payload = JsonSerializer.Serialize(notificationIntent);
				Intent launchIntent = new(Android.App.Application.Context, typeof(MainActivity));
				launchIntent.SetAction(Intent.ActionView);
				launchIntent.PutExtra("notificationIntent", payload);
				launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

				int requestCode = Math.Abs(payload.GetHashCode());
				PendingIntent pendingIntent = PendingIntent.GetActivity(
					Android.App.Application.Context,
					requestCode,
					launchIntent,
					PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable | PendingIntentFlags.OneShot);

				builder.SetContentIntent(pendingIntent);
			}
			catch (Exception)
			{
				// If serialization fails, fall back to a notification without tap handling.
			}

			manager.Notify(Guid.NewGuid().GetHashCode(), builder.Build());
			return Task.CompletedTask;
		}

		private void EnsureChannel(NotificationManager manager, string channelId)
		{
			if (this.createdChannels.Contains(channelId) || !OperatingSystem.IsAndroidVersionAtLeast(26))
				return;

			NotificationChannel channel = new(channelId, channelId, NotificationImportance.High)
			{
				LockscreenVisibility = NotificationVisibility.Private
			};
			manager.CreateNotificationChannel(channel);
			this.createdChannels.Add(channelId);
		}
	}
}
