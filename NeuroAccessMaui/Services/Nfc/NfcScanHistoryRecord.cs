using System;
using NeuroAccessMaui.Services.Nfc.Ui;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Nfc
{
	/// <summary>
	/// Persisted NFC scan history record.
	/// </summary>
	[CollectionName("NfcScanHistory")]
	[TypeName(TypeNameSerialization.None)]
	[Index(nameof(DetectedAtUtc))]
	[Index(nameof(TagId))]
	[Index(nameof(ExtractedUri))]
	public sealed class NfcScanHistoryRecord
	{
		private string? objectId;
		private DateTime detectedAtUtc;
		private CaseInsensitiveString tagId = CaseInsensitiveString.Empty;
		private string tagType = string.Empty;
		private string interfacesSummary = string.Empty;
		private string? ndefSummary;
		private string? extractedUri;
		private NfcNdefRecordSnapshot[]? ndefRecords;

		/// <summary>
		/// Gets or sets the object identifier.
		/// </summary>
		[ObjectId]
		public string? ObjectId
		{
			get => this.objectId;
			set => this.objectId = value;
		}

		/// <summary>
		/// Gets or sets the UTC timestamp when the tag was detected.
		/// </summary>
		public DateTime DetectedAtUtc
		{
			get => this.detectedAtUtc;
			set => this.detectedAtUtc = value;
		}

		/// <summary>
		/// Gets or sets the tag identifier.
		/// </summary>
		public CaseInsensitiveString TagId
		{
			get => this.tagId;
			set => this.tagId = value;
		}

		/// <summary>
		/// Gets or sets the high-level tag type.
		/// </summary>
		public string TagType
		{
			get => this.tagType;
			set => this.tagType = value ?? string.Empty;
		}

		/// <summary>
		/// Gets or sets a summary of detected tag technologies.
		/// </summary>
		public string InterfacesSummary
		{
			get => this.interfacesSummary;
			set => this.interfacesSummary = value ?? string.Empty;
		}

		/// <summary>
		/// Gets or sets an optional NDEF summary.
		/// </summary>
		[DefaultValueNull]
		public string? NdefSummary
		{
			get => this.ndefSummary;
			set => this.ndefSummary = value;
		}

		/// <summary>
		/// Gets or sets an optional extracted URI.
		/// </summary>
		[DefaultValueNull]
		public string? ExtractedUri
		{
			get => this.extractedUri;
			set => this.extractedUri = value;
		}

		/// <summary>
		/// Gets or sets decoded NDEF record details (best-effort).
		/// </summary>
		[DefaultValueNull]
		public NfcNdefRecordSnapshot[]? NdefRecords
		{
			get => this.ndefRecords;
			set => this.ndefRecords = value;
		}
	}
}
