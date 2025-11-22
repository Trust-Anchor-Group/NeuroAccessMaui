using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Notification;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Renders local notifications on the platform.
	/// </summary>
	public interface INotificationRenderer
	{
		/// <summary>
		/// Displays a local notification for the provided intent.
		/// </summary>
		/// <param name="Intent">Notification intent to render.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task RenderAsync(NotificationIntent Intent, CancellationToken CancellationToken);
	}
}
