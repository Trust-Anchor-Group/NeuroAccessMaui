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
		/// <param name="Predicate">Predicate to decide which aspects of handling to ignore.</param>
		/// <returns>Disposable handle that removes the filter.</returns>
		IDisposable AddFilter(Func<NotificationIntent, NotificationFilterDecision> Predicate);

		/// <summary>
		/// Returns filter decisions for the notification.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <param name="FromUserInteraction">If triggered by user interaction.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		NotificationFilterDecision ShouldIgnore(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken);
	}
}
