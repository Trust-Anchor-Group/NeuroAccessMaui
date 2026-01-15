using System;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Stores and publishes the last observed NFC tag snapshot for UI consumption.
	/// </summary>
	[DefaultImplementation(typeof(NfcTagSnapshotService))]
	public interface INfcTagSnapshotService
	{
		/// <summary>
		/// Occurs when <see cref="LastSnapshot"/> changes.
		/// </summary>
		event EventHandler? SnapshotChanged;

		/// <summary>
		/// Gets the last observed NFC tag snapshot.
		/// </summary>
		NfcTagSnapshot? LastSnapshot { get; }

		/// <summary>
		/// Publishes a new snapshot.
		/// </summary>
		/// <param name="Snapshot">Snapshot to publish.</param>
		void Publish(NfcTagSnapshot Snapshot);

		/// <summary>
		/// Updates NDEF-related fields for the last snapshot if it matches the provided tag id.
		/// </summary>
		/// <param name="TagIdHex">Tag identifier represented as a hex string.</param>
		/// <param name="NdefSummary">Optional NDEF summary.</param>
		/// <param name="ExtractedUri">Optional extracted URI.</param>
		void UpdateNdef(string TagIdHex, string? NdefSummary, string? ExtractedUri);

		/// <summary>
		/// Clears the last snapshot.
		/// </summary>
		void Clear();
	}
}
