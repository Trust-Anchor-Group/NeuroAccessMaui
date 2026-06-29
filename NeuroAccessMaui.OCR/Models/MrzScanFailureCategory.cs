namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Describes the main failure category for an MRZ scan.
	/// </summary>
	public enum MrzScanFailureCategory
	{
		/// <summary>
		/// No categorized failure occurred.
		/// </summary>
		None = 0,

		/// <summary>
		/// No usable document page was found.
		/// </summary>
		DocumentNotFound = 1,

		/// <summary>
		/// The document image was too blurred for reliable recognition.
		/// </summary>
		DocumentTooBlurred = 2,

		/// <summary>
		/// Glare intersects the document or MRZ area.
		/// </summary>
		GlareInMrzArea = 3,

		/// <summary>
		/// A document was found but no plausible MRZ region was localized.
		/// </summary>
		MrzRegionNotFound = 4,

		/// <summary>
		/// OCR returned no useful text for the selected MRZ candidate.
		/// </summary>
		OcrNoText = 5,

		/// <summary>
		/// OCR text was found but MRZ validation failed.
		/// </summary>
		MrzChecksumFailed = 6,

		/// <summary>
		/// The visible document side does not appear to contain the requested MRZ.
		/// </summary>
		UnsupportedDocumentSide = 7
	}
}
