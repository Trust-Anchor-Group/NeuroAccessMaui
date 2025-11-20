using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Renders local notifications on the platform.
	/// </summary>
	public interface INotificationRenderer
	{
		/// <summary>
		/// Displays a local notification with the provided title and message.
		/// </summary>
		/// <param name="Title">Notification title.</param>
		/// <param name="Message">Notification body.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task RenderAsync(string Title, string? Message, CancellationToken CancellationToken);
	}
}
