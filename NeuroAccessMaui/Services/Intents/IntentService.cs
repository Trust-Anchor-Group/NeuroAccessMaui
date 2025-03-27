using System.Collections.Concurrent;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Nfc;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Intents
{
	/// <summary>
	/// A service that handles application intents by queuing them and processing them when the application is ready.
	/// This implementation is designed to work across both Android and iOS.
	/// </summary>
	[Singleton]
	public class IntentService : IIntentService
	{
		// Thread-safe queue for storing incoming intents. Usually there will always be only one or none intents in the queue.
		// ConcurrentQueue might not be needed, but this is not performance-critical code.
		private readonly ConcurrentQueue<AppIntent> intentQueue = new();

		/// <summary>
		/// Queues an intent for later processing.
		/// </summary>
		/// <param name="intent">The intent to be queued.</param>
		public void QueueIntent(AppIntent intent)
		{
			try
			{
				this.intentQueue.Enqueue(intent);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Processes all queued intents asynchronously.
		/// This method will dequeue and process each intent until the queue is empty.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public async Task ProcessQueuedIntentsAsync()
		{
			try
			{
				while (this.intentQueue.TryDequeue(out AppIntent? Intent))
				{
					await this.ProcessIntentAsync(Intent);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Processes a single intent asynchronously.
		/// </summary>
		/// <param name="intent">The intent to process.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public async Task ProcessIntentAsync(AppIntent intent)
		{
			try
			{
				switch (intent.Action)
				{
					case Constants.IntentActions.OpenUrl:
						if (!string.IsNullOrEmpty(intent.Data))
						{
							// Process a url.
							App.OpenUrlSync(intent.Data);
						}
						break;

					case Constants.IntentActions.NfcTagDiscovered:
						if (intent.Payload is NfcTag NfcTag)
						{
							// Resolve your shared NFC service and pass the NFC tag.
							INfcService NfcService = App.Instantiate<INfcService>();
							await NfcService.TagDetected(NfcTag);
						}
						break;

					default:
						ServiceRef.LogService.LogWarning($"Unhandled intent action: {intent.Action}");
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
