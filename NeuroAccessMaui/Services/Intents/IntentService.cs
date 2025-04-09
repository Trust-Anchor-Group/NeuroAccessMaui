using System.Collections.Concurrent;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Nfc;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Intents
{
	/// <summary>
	/// The <c>IntentService</c> class is responsible for handling and processing application intents in a cross‑platform manner.
	/// <para>
	/// An "intent" is a message or signal that informs the application that a certain event has occurred, and that specific action should be taken.
	/// For example, an intent may indicate that the application should open a URL (for deep linking) or that an NFC tag has been detected.
	/// In mobile development, intents are used to decouple platform‑specific event detection (e.g., Android's <c>MainActivity</c> or iOS's <c>AppDelegate</c>)
	/// from the shared business logic that processes these events.
	/// </para>
	/// <para>
	/// This service queues incoming intents until the application is fully initialized and ready to process them.
	/// It supports asynchronous processing, allowing the application to handle events such as deep links, NFC tag detections,
	/// or other custom actions in a unified and decoupled manner.
	/// </para>
	/// <para>
	/// Platform‑specific code should create an <c>AppIntent</c> instance with an appropriate action, data, and/or payload,
	/// and then call <see cref="QueueIntent(AppIntent)"/> to add the intent to the queue.
	/// Once the application is ready (for example, during startup in the shared <c>App</c> class), 
	/// <see cref="ProcessQueuedIntentsAsync"/> should be called to process all queued intents.
	/// </para>
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
