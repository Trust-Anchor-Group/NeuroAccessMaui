using System.Collections.Generic;
using NeuroAccess.Nfc.TravelDocuments;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents the MRZ text read from a single image, after decoding and check-digit validation.
	/// </summary>
	public sealed class MrzFrameRead
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzFrameRead"/> class.
		/// </summary>
		/// <param name="Format">The decoded MRZ layout.</param>
		/// <param name="Lines">The decoded MRZ lines, each padded or trimmed to the layout's line length.</param>
		/// <param name="RawText">The text returned by the recognizer before decoding.</param>
		/// <param name="RecognitionConfidence">The mean character confidence reported by the recognizer.</param>
		/// <param name="DecodeScore">The decoder's ranking score for the selected candidate.</param>
		/// <param name="IsStrictSuccess">If every check digit, including the composite check, passed.</param>
		/// <param name="PassedCheckCount">The number of required check digits that passed.</param>
		/// <param name="RepairCount">The number of characters changed to satisfy field check digits.</param>
		/// <param name="Document">The parsed document information, when validation succeeded.</param>
		/// <param name="Metadata">Additional decoding metadata.</param>
		public MrzFrameRead(
			MrzDocumentFormat Format,
			IReadOnlyList<string> Lines,
			string RawText,
			float RecognitionConfidence,
			float DecodeScore,
			bool IsStrictSuccess,
			int PassedCheckCount,
			int RepairCount,
			DocumentInformation? Document,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Lines);
			ArgumentNullException.ThrowIfNull(RawText);

			this.Format = Format;
			this.Lines = Lines;
			this.NormalizedText = string.Join("\n", Lines);
			this.RawText = RawText;
			this.RecognitionConfidence = RecognitionConfidence;
			this.DecodeScore = DecodeScore;
			this.IsStrictSuccess = IsStrictSuccess;
			this.PassedCheckCount = PassedCheckCount;
			this.RepairCount = RepairCount;
			this.Document = Document;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the decoded MRZ layout.
		/// </summary>
		public MrzDocumentFormat Format { get; }

		/// <summary>
		/// Gets the decoded MRZ lines, each padded or trimmed to the layout's line length.
		/// </summary>
		public IReadOnlyList<string> Lines { get; }

		/// <summary>
		/// Gets the decoded MRZ lines joined by line breaks.
		/// </summary>
		public string NormalizedText { get; }

		/// <summary>
		/// Gets the text returned by the recognizer before decoding.
		/// </summary>
		public string RawText { get; }

		/// <summary>
		/// Gets the mean character confidence reported by the recognizer.
		/// </summary>
		public float RecognitionConfidence { get; }

		/// <summary>
		/// Gets the decoder's ranking score for the selected candidate.
		/// </summary>
		public float DecodeScore { get; }

		/// <summary>
		/// Gets a value indicating whether every check digit, including the composite check, passed.
		/// </summary>
		public bool IsStrictSuccess { get; }

		/// <summary>
		/// Gets the number of required check digits that passed.
		/// </summary>
		public int PassedCheckCount { get; }

		/// <summary>
		/// Gets the number of characters changed to satisfy field check digits.
		/// </summary>
		public int RepairCount { get; }

		/// <summary>
		/// Gets the parsed document information, when validation succeeded.
		/// </summary>
		public DocumentInformation? Document { get; }

		/// <summary>
		/// Gets additional decoding metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
