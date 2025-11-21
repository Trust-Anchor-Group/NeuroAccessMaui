using System;
using System.Threading;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Manages runtime notification filters (ignore rules).
	/// </summary>
	public interface INotificationFilterRegistry
	{
		/// <summary>
		/// Registers a filter predicate; returns a handle that removes the filter when disposed.
		/// </summary>
		/// <param name="Predicate">Predicate to decide if a notification intent should be ignored.</param>
		/// <returns>Disposable handle that removes the filter.</returns>
		IDisposable AddFilter(Func<NotificationIntent, bool> Predicate);

		/// <summary>
		/// Returns true if any active filter chooses to ignore the notification.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <param name="FromUserInteraction">If triggered by user interaction.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		bool ShouldIgnore(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken);
	}
}
