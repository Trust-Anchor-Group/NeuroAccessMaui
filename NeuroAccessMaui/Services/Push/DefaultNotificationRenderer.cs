using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Notification;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// No-op renderer used until platform-specific implementations are provided.
	/// </summary>
	public sealed class DefaultNotificationRenderer : INotificationRenderer
	{
		/// <summary>
		/// Displays a local notification with the provided intent.
		/// </summary>
		/// <param name="Intent">Notification intent to render.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Task representing the asynchronous operation.</returns>
		public Task RenderAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			_ = Intent;
			CancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}
	}
}
