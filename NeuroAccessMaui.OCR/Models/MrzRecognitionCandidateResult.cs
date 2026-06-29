using System.Collections.Generic;
using NeuroAccess.Nfc.TravelDocuments;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents OCR and parser output for one MRZ visual candidate.
	/// </summary>
	public sealed class MrzRecognitionCandidateResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzRecognitionCandidateResult"/> class.
		/// </summary>
		/// <param name="Candidate">The visual MRZ candidate.</param>
		/// <param name="RawText">The raw OCR text.</param>
		/// <param name="NormalizedText">The normalized MRZ text.</param>
		/// <param name="DocumentInformation">The parsed document information, if available.</param>
		/// <param name="Format">The MRZ format, if decoded.</param>
		/// <param name="Confidence">The confidence score.</param>
		/// <param name="IsStrictSuccess">If the parser accepted the result strictly.</param>
		/// <param name="FailureCategory">The failure category, if any.</param>
		/// <param name="FailureReason">The failure reason, if any.</param>
		/// <param name="Metadata">Additional recognition metadata.</param>
		public MrzRecognitionCandidateResult(
			MrzRegionCandidate Candidate,
			string RawText,
			string NormalizedText,
			DocumentInformation? DocumentInformation,
			MrzDocumentFormat? Format,
			float Confidence,
			bool IsStrictSuccess,
			MrzScanFailureCategory FailureCategory,
			string? FailureReason,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Candidate);
			ArgumentNullException.ThrowIfNull(RawText);
			ArgumentNullException.ThrowIfNull(NormalizedText);

			this.Candidate = Candidate;
			this.RawText = RawText;
			this.NormalizedText = NormalizedText;
			this.DocumentInformation = DocumentInformation;
			this.Format = Format;
			this.Confidence = Confidence;
			this.IsStrictSuccess = IsStrictSuccess;
			this.FailureCategory = FailureCategory;
			this.FailureReason = FailureReason;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the visual MRZ candidate.
		/// </summary>
		public MrzRegionCandidate Candidate { get; }

		/// <summary>
		/// Gets the raw OCR text.
		/// </summary>
		public string RawText { get; }

		/// <summary>
		/// Gets the normalized MRZ text.
		/// </summary>
		public string NormalizedText { get; }

		/// <summary>
		/// Gets the parsed document information, if available.
		/// </summary>
		public DocumentInformation? DocumentInformation { get; }

		/// <summary>
		/// Gets the MRZ format, if decoded.
		/// </summary>
		public MrzDocumentFormat? Format { get; }

		/// <summary>
		/// Gets the confidence score.
		/// </summary>
		public float Confidence { get; }

		/// <summary>
		/// Gets a value indicating whether the parser accepted the result strictly.
		/// </summary>
		public bool IsStrictSuccess { get; }

		/// <summary>
		/// Gets the failure category, if any.
		/// </summary>
		public MrzScanFailureCategory FailureCategory { get; }

		/// <summary>
		/// Gets the failure reason, if any.
		/// </summary>
		public string? FailureReason { get; }

		/// <summary>
		/// Gets additional recognition metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
