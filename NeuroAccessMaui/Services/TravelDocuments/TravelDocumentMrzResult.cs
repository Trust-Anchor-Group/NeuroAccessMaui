using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Represents the result of MRZ scanning and validation.
	/// </summary>
	public sealed class TravelDocumentMrzResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TravelDocumentMrzResult"/> class.
		/// </summary>
		/// <param name="OcrResult">The underlying OCR result.</param>
		/// <param name="Document">The parsed document information, if available.</param>
		public TravelDocumentMrzResult(OcrScanResult OcrResult, DocumentInformation? Document)
		{
			ArgumentNullException.ThrowIfNull(OcrResult);

			this.OcrResult = OcrResult;
			this.Document = Document;
		}

		/// <summary>
		/// Gets the underlying OCR result.
		/// </summary>
		public OcrScanResult OcrResult { get; }

		/// <summary>
		/// Gets the parsed document information, if available.
		/// </summary>
		public DocumentInformation? Document { get; }

		/// <summary>
		/// Gets the full normalized MRZ text recognized by OCR.
		/// </summary>
		public string NormalizedMrzText => this.OcrResult.Mrz?.NormalizedText?.Trim() ?? string.Empty;

		/// <summary>
		/// Gets a value indicating whether the MRZ result is valid for NFC handoff.
		/// </summary>
		public bool IsSuccessful =>
			!string.IsNullOrWhiteSpace(this.NormalizedMrzText) &&
			this.Document?.MRZ_Information is not null;
	}
}
