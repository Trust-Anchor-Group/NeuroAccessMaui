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
		private readonly HashSet<string> createdChannels = new HashSet<string>();

		/// <summary>
		/// Displays a local notification with the provided title and message.
		/// </summary>
		/// <param name="NotificationIntent">Notification data to render.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous render operation.</returns>
		public Task RenderAsync(NotificationIntent NotificationIntent, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(NotificationIntent);
			CancellationToken.ThrowIfCancellationRequested();

			if (NotificationIntent.Presentation is NotificationPresentation.StoreOnly or NotificationPresentation.Transient)
			{
				return Task.CompletedTask;
			}

			Context? ApplicationContext = Android.App.Application.Context;
			if (ApplicationContext is null)
			{
				return Task.CompletedTask;
			}
			Context ApplicationContextInstance = ApplicationContext;

			string ChannelId = string.IsNullOrWhiteSpace(NotificationIntent.Channel) ? DefaultChannelId : NotificationIntent.Channel!;

			NotificationManager? NotificationManagerInstance = ApplicationContextInstance.GetSystemService(Context.NotificationService) as NotificationManager;
			if (NotificationManagerInstance is null)
			{
				return Task.CompletedTask;
			}
			NotificationManager NotificationManager = NotificationManagerInstance;

			this.EnsureChannel(NotificationManager, ChannelId);

			int IconId = ApplicationContextInstance.Resources?.GetIdentifier(
			"ic_stat_appiconfg",
			"drawable",
			ApplicationContextInstance.PackageName) ?? ApplicationContextInstance.ApplicationInfo?.Icon ?? Android.Resource.Drawable.SymDefAppIcon;
			NotificationCompat.Builder? Builder = new NotificationCompat.Builder(ApplicationContextInstance, ChannelId)
				.SetContentTitle(NotificationIntent.Title ?? string.Empty)?
				.SetContentText(NotificationIntent.Body ?? string.Empty)?
				.SetSmallIcon(IconId)?
				.SetAutoCancel(true)?
				.SetPriority((int)NotificationPriority.High);

			if (OperatingSystem.IsAndroidVersionAtLeast(26))
			{
				Builder?.SetCategory(AndroidNotification.CategoryMessage);
			}

			try
			{
				string Payload = JsonSerializer.Serialize(NotificationIntent);
				Intent LaunchIntent = new Intent(ApplicationContextInstance, typeof(MainActivity));
				LaunchIntent.SetAction(Intent.ActionView);
				LaunchIntent.PutExtra("notificationIntent", Payload);
				LaunchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

				int RequestCode = Math.Abs(Payload.GetHashCode());
				PendingIntent? PendingIntentInstance = PendingIntent.GetActivity(
					ApplicationContextInstance,
					RequestCode,
					LaunchIntent,
					PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable | PendingIntentFlags.OneShot);

				if (PendingIntentInstance is not null)
				{
					Builder?.SetContentIntent(PendingIntentInstance);
				}
			}
			catch (Exception)
			{
				// If serialization fails, fall back to a notification without tap handling.
			}

			if (Builder is null)
				return Task.CompletedTask;

			NotificationManager.Notify(Guid.NewGuid().GetHashCode(), Builder.Build());
			return Task.CompletedTask;
		}

		private void EnsureChannel(NotificationManager NotificationManagerInstance, string ChannelId)
		{
			if (this.createdChannels.Contains(ChannelId) || !OperatingSystem.IsAndroidVersionAtLeast(26))
				return;

			NotificationChannel Channel = new NotificationChannel(ChannelId, ChannelId, NotificationImportance.High)
			{
				LockscreenVisibility = NotificationVisibility.Private
			};
			NotificationManagerInstance.CreateNotificationChannel(Channel);
			this.createdChannels.Add(ChannelId);
		}
	}
}
