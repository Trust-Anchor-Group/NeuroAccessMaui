using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Routes notification intents to navigation targets.
	/// </summary>
	public interface INotificationIntentRouter
	{
		/// <summary>
		/// Routes a notification intent.
		/// </summary>
		/// <param name="Intent">Notification intent.</param>
		/// <param name="FromUserInteraction">If true, routing was triggered by user interaction.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Routing result.</returns>
		Task<NotificationRouteResult> RouteAsync(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken);
	}
}
