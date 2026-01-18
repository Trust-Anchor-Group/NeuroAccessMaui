using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Provides a user-initiated NFC write workflow.
	/// </summary>
	[DefaultImplementation(typeof(NfcWriteService))]
	public interface INfcWriteService
	{
		/// <summary>
		/// Gets a value indicating whether a write operation is pending.
		/// </summary>
		bool HasPendingWrite { get; }

		/// <summary>
		/// Begins writing a URI to the next detected writable NFC tag.
		/// </summary>
		/// <param name="Uri">Absolute URI to write.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>True if the write succeeded; otherwise false.</returns>
		Task<bool> WriteUriAsync(string Uri, CancellationToken CancellationToken);

		/// <summary>
		/// Begins writing a text record to the next detected writable NFC tag.
		/// </summary>
		/// <param name="Text">Text to write.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>True if the write succeeded; otherwise false.</returns>
		Task<bool> WriteTextAsync(string Text, CancellationToken CancellationToken);

		/// <summary>
		/// Attempts to apply a pending write request to a detected tag.
		/// </summary>
		/// <param name="WriteCallback">Callback that writes the prepared NDEF items to the tag.</param>
		/// <returns>True if a pending write was consumed; otherwise false.</returns>
		Task<bool> TryHandleWriteAsync(NfcService.WriteItems WriteCallback);

		/// <summary>
		/// Cancels any pending write operation.
		/// </summary>
		void CancelPendingWrite();
	}
}
