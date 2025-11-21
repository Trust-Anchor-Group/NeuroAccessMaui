using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidNotification = Android.App.Notification;

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
		public Task RenderAsync(string Title, string? Message, string Channel, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string channelId = string.IsNullOrWhiteSpace(Channel) ? DefaultChannelId : Channel;

			NotificationManager? manager = Android.App.Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
			if (manager is null)
				return Task.CompletedTask;

			this.EnsureChannel(manager, channelId);

			int iconId = Android.App.Application.Context?.ApplicationInfo?.Icon ?? Android.Resource.Drawable.SymDefAppIcon;

			NotificationCompat.Builder builder = new NotificationCompat.Builder(Android.App.Application.Context, channelId)
				.SetContentTitle(Title)
				.SetContentText(Message ?? string.Empty)
				.SetSmallIcon(iconId)
				.SetAutoCancel(true)
				.SetPriority((int)NotificationPriority.High);

			if (OperatingSystem.IsAndroidVersionAtLeast(26))
			{
				builder.SetCategory(AndroidNotification.CategoryMessage);
			}

			manager.Notify(System.Guid.NewGuid().GetHashCode(), builder.Build());
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
