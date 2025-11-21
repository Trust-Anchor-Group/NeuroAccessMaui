using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UserNotifications;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Renders local notifications on iOS.
	/// </summary>
	public sealed class IosNotificationRenderer : INotificationRenderer
	{
		/// <summary>
		/// Displays a local notification with the provided title and message.
		/// </summary>
		/// <param name="Title">Notification title.</param>
		/// <param name="Message">Notification body.</param>
		/// <param name="Channel">Notification channel identifier (ignored on iOS).</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public async Task RenderAsync(string Title, string? Message, string Channel, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			_ = Channel;

			UNMutableNotificationContent content = new()
			{
				Title = Title,
				Body = Message ?? string.Empty,
				Sound = UNNotificationSound.Default
			};

			UNNotificationRequest request = UNNotificationRequest.FromIdentifier(
				NSUuid.NewUuid().AsString(),
				content,
				trigger: null);

			await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
		}
	}
}
