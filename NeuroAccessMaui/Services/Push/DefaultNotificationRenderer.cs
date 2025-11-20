using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// No-op renderer used until platform-specific implementations are provided.
	/// </summary>
	public sealed class DefaultNotificationRenderer : INotificationRenderer
	{
		/// <summary>
		/// Displays a local notification with the provided title and message.
		/// </summary>
		/// <param name="Title">Notification title.</param>
		/// <param name="Message">Notification body.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Task representing the asynchronous operation.</returns>
		public Task RenderAsync(string Title, string? Message, CancellationToken CancellationToken)
		{
			_ = Title;
			_ = Message;
			CancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}
	}
}
