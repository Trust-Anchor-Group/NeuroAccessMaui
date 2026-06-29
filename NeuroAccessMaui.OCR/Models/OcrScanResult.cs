namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents the final outcome of a simplified OCR scan.
	/// </summary>
	public sealed class OcrScanResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OcrScanResult"/> class.
		/// </summary>
		/// <param name="TargetKind">The requested scan target.</param>
		/// <param name="SourceKind">The source kind.</param>
		/// <param name="ProviderId">The provider that handled the scan.</param>
		/// <param name="ValidationStatus">The final validation status.</param>
		/// <param name="RawText">The raw OCR text.</param>
		/// <param name="NormalizedText">The normalized target text.</param>
		/// <param name="Mrz">The parsed MRZ result, if valid.</param>
		/// <param name="Confidence">The result confidence.</param>
		/// <param name="FailureReason">The failure reason, if any.</param>
		/// <param name="Metadata">Additional result metadata.</param>
		/// <param name="ArtifactBundle">The persisted debug-artifact bundle, if any.</param>
		public OcrScanResult(
			OcrScanTargetKind TargetKind,
			OcrScanSourceKind SourceKind,
			string ProviderId,
			OcrScanValidationStatus ValidationStatus,
			string RawText,
			string NormalizedText,
			MrzScanResult? Mrz,
			float Confidence,
			string? FailureReason = null,
			IReadOnlyDictionary<string, string>? Metadata = null,
			OcrArtifactBundle? ArtifactBundle = null)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(ProviderId);
			ArgumentNullException.ThrowIfNull(RawText);
			ArgumentNullException.ThrowIfNull(NormalizedText);

			this.TargetKind = TargetKind;
			this.SourceKind = SourceKind;
			this.ProviderId = ProviderId;
			this.ValidationStatus = ValidationStatus;
			this.RawText = RawText;
			this.NormalizedText = NormalizedText;
			this.Mrz = Mrz;
			this.Confidence = Confidence;
			this.FailureReason = FailureReason;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
			this.ArtifactBundle = ArtifactBundle;
		}

		/// <summary>
		/// Gets the requested scan target.
		/// </summary>
		public OcrScanTargetKind TargetKind { get; }

		/// <summary>
		/// Gets the source kind.
		/// </summary>
		public OcrScanSourceKind SourceKind { get; }

		/// <summary>
		/// Gets the provider that handled the scan.
		/// </summary>
		public string ProviderId { get; }

		/// <summary>
		/// Gets the final validation status.
		/// </summary>
		public OcrScanValidationStatus ValidationStatus { get; }

		/// <summary>
		/// Gets the raw OCR text.
		/// </summary>
		public string RawText { get; }

		/// <summary>
		/// Gets the normalized target text.
		/// </summary>
		public string NormalizedText { get; }

		/// <summary>
		/// Gets the parsed MRZ result when validation succeeded.
		/// </summary>
		public MrzScanResult? Mrz { get; }

		/// <summary>
		/// Gets the overall confidence score.
		/// </summary>
		public float Confidence { get; }

		/// <summary>
		/// Gets the failure reason, if any.
		/// </summary>
		public string? FailureReason { get; }

		/// <summary>
		/// Gets additional result metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }

		/// <summary>
		/// Gets the persisted debug-artifact bundle, if any.
		/// </summary>
		public OcrArtifactBundle? ArtifactBundle { get; }

		/// <summary>
		/// Gets a value indicating whether the scan succeeded with a strictly valid result.
		/// </summary>
		public bool IsSuccessful => this.ValidationStatus == OcrScanValidationStatus.Succeeded;
	}
}
