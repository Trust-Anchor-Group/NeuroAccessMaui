using System;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Represents a snapshot of the last NFC tag observed by the application.
	/// </summary>
	public sealed class NfcTagSnapshot
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcTagSnapshot"/> class.
		/// </summary>
		/// <param name="TagIdHex">Tag identifier represented as a hex string.</param>
		/// <param name="DetectedAtUtc">Timestamp when the tag was detected (UTC).</param>
		/// <param name="TagType">High-level tag type classification (best-effort).</param>
		/// <param name="InterfacesSummary">Human-readable summary of available tag interfaces/technologies.</param>
		/// <param name="NdefSummary">Optional summary of decoded NDEF records.</param>
		/// <param name="ExtractedUri">Optional extracted URI (if present).</param>
		/// <param name="NdefRecords">Optional decoded NDEF record details (best-effort).</param>
		public NfcTagSnapshot(
			string TagIdHex,
			DateTimeOffset DetectedAtUtc,
			string TagType,
			string InterfacesSummary,
			string? NdefSummary,
			string? ExtractedUri,
			IReadOnlyList<NfcNdefRecordSnapshot>? NdefRecords = null)
		{
			this.TagIdHex = TagIdHex;
			this.DetectedAtUtc = DetectedAtUtc;
			this.TagType = TagType;
			this.InterfacesSummary = InterfacesSummary;
			this.NdefSummary = NdefSummary;
			this.ExtractedUri = ExtractedUri;
			this.NdefRecords = NdefRecords;
		}

		/// <summary>
		/// Gets the tag identifier represented as a hex string.
		/// </summary>
		public string TagIdHex { get; }

		/// <summary>
		/// Gets the timestamp when the tag was detected (UTC).
		/// </summary>
		public DateTimeOffset DetectedAtUtc { get; }

		/// <summary>
		/// Gets the high-level tag type classification (best-effort).
		/// </summary>
		public string TagType { get; }

		/// <summary>
		/// Gets a human-readable summary of available tag interfaces/technologies.
		/// </summary>
		public string InterfacesSummary { get; }

		/// <summary>
		/// Gets an optional summary of decoded NDEF records.
		/// </summary>
		public string? NdefSummary { get; }

		/// <summary>
		/// Gets an optional extracted URI.
		/// </summary>
		public string? ExtractedUri { get; }

		/// <summary>
		/// Gets optional decoded NDEF record details (best-effort).
		/// </summary>
		public IReadOnlyList<NfcNdefRecordSnapshot>? NdefRecords { get; }

		/// <summary>
		/// Creates a copy with updated NDEF-related data.
		/// </summary>
		/// <param name="NdefSummary">New NDEF summary.</param>
		/// <param name="ExtractedUri">New extracted URI.</param>
		/// <returns>Updated snapshot instance.</returns>
		public NfcTagSnapshot WithNdef(string? NdefSummary, string? ExtractedUri)
		{
			return this.WithNdefDetails(NdefSummary, ExtractedUri, null);
		}

		/// <summary>
		/// Creates a copy with updated NDEF-related data.
		/// </summary>
		/// <param name="NdefSummary">New NDEF summary.</param>
		/// <param name="ExtractedUri">New extracted URI.</param>
		/// <param name="NdefRecords">New decoded NDEF records.</param>
		/// <returns>Updated snapshot instance.</returns>
		public NfcTagSnapshot WithNdefDetails(string? NdefSummary, string? ExtractedUri, IReadOnlyList<NfcNdefRecordSnapshot>? NdefRecords)
		{
			return new NfcTagSnapshot(
				this.TagIdHex,
				this.DetectedAtUtc,
				this.TagType,
				this.InterfacesSummary,
				NdefSummary,
				ExtractedUri,
				NdefRecords);
		}
	}
}
