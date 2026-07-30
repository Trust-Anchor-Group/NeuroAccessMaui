namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Describes the caller's best knowledge of the document side being scanned.
	/// </summary>
	public enum OcrDocumentSideHint
	{
		/// <summary>
		/// The document side is not known.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// The front side is expected.
		/// </summary>
		Front = 1,

		/// <summary>
		/// The back side is expected.
		/// </summary>
		Back = 2,

		/// <summary>
		/// A passport or document biodata page is expected.
		/// </summary>
		BiodataPage = 3
	}
}
