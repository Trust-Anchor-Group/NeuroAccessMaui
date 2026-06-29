using NeuroAccess.Nfc.TravelDocuments;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Contains parsed MRZ data returned by a successful scan.
	/// </summary>
	public sealed class MrzScanResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzScanResult"/> class.
		/// </summary>
		/// <param name="Format">The detected MRZ format.</param>
		/// <param name="NormalizedText">The normalized MRZ text.</param>
		/// <param name="Document">The parsed document information.</param>
		public MrzScanResult(MrzDocumentFormat Format, string NormalizedText, DocumentInformation Document)
		{
			ArgumentNullException.ThrowIfNull(NormalizedText);
			ArgumentNullException.ThrowIfNull(Document);

			this.Format = Format;
			this.NormalizedText = NormalizedText;
			this.Document = Document;
		}

		/// <summary>
		/// Gets the detected MRZ format.
		/// </summary>
		public MrzDocumentFormat Format { get; }

		/// <summary>
		/// Gets the normalized MRZ text.
		/// </summary>
		public string NormalizedText { get; }

		/// <summary>
		/// Gets the parsed document information.
		/// </summary>
		public DocumentInformation Document { get; }
	}
}
