using System.Collections.Concurrent;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Intents
{
	/// <summary>
	/// Represents an application intent containing an action, optional data, and an optional payload.
	/// </summary>
	public class AppIntent
	{
		/// <summary>
		/// Gets or sets the action associated with the intent.
		/// </summary>
		public string Action { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the optional string data associated with the intent.
		/// This might be used for deep links.
		/// </summary>
		public string? Data { get; set; }

		/// <summary>
		/// Gets or sets an optional payload containing additional information (e.g. a cross‑platform NFC tag).
		/// </summary>
		public object? Payload { get; set; }
	}

	/// <summary>
	/// Defines the contract for a service that handles application intents.
	/// </summary>
	[DefaultImplementation(typeof(IntentService))]
	public interface IIntentService
	{
		/// <summary>
		/// Queues an intent for later processing.
		/// </summary>
		/// <param name="intent">The intent to be queued.</param>
		void QueueIntent(AppIntent intent);

		/// <summary>
		/// Processes all queued intents asynchronously.
		/// This method should be called once the application is fully initialized.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		Task ProcessQueuedIntentsAsync();

		/// <summary>
		/// Processes a single intent asynchronously.
		/// </summary>
		/// <param name="intent">The intent to process.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ProcessIntentAsync(AppIntent intent);

	}
}
