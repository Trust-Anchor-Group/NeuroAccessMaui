using System.Threading;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Determines if a notification should be ignored based on current app context.
	/// </summary>
	public interface INotificationFilter
	{
		/// <summary>
		/// Returns true if the notification should be ignored.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <param name="FromUserInteraction">If the notification came from explicit user interaction.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		bool ShouldIgnore(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken);
	}
}
