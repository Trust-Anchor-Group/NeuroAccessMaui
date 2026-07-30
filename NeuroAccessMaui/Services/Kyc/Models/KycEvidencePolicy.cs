namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Defines evidence requirements for a KYC process.
	/// </summary>
	public class KycEvidencePolicy
	{
		/// <summary>
		/// Gets or sets the travel-document evidence policy.
		/// </summary>
		public KycTravelDocumentEvidencePolicy TravelDocument { get; set; } = new KycTravelDocumentEvidencePolicy();
	}

	/// <summary>
	/// Defines travel-document evidence requirements for a KYC process.
	/// </summary>
	public class KycTravelDocumentEvidencePolicy
	{
		/// <summary>
		/// Gets or sets a value indicating whether travel-document evidence is required.
		/// </summary>
		public bool Required { get; set; }

		/// <summary>
		/// Gets or sets the NFC evidence policy.
		/// </summary>
		public KycNfcEvidencePolicy Nfc { get; set; } = new KycNfcEvidencePolicy();
	}

	/// <summary>
	/// Defines NFC evidence requirements and attachment metadata.
	/// </summary>
	public class KycNfcEvidencePolicy
	{
		/// <summary>
		/// Gets the default NFC evidence attachment name.
		/// </summary>
		public const string DefaultAttachmentName = "NFC.xml";

		/// <summary>
		/// Gets the default NFC evidence content type.
		/// </summary>
		public const string DefaultContentType = "text/xml";

		/// <summary>
		/// Gets or sets a value indicating whether NFC evidence is enabled.
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether NFC evidence is required before submission.
		/// </summary>
		public bool Required { get; set; }

		/// <summary>
		/// Gets or sets the attachment name used for NFC evidence.
		/// </summary>
		public string AttachmentName { get; set; } = DefaultAttachmentName;

		/// <summary>
		/// Gets or sets the content type used for NFC evidence.
		/// </summary>
		public string ContentType { get; set; } = DefaultContentType;
	}
}
