namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Describes the caller's best knowledge of the document family being scanned.
	/// </summary>
	public enum OcrDocumentKindHint
	{
		/// <summary>
		/// The document family is not known.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// The document is expected to be a passport biodata page.
		/// </summary>
		Passport = 1,

		/// <summary>
		/// The document is expected to be a national identity card.
		/// </summary>
		IdentityCard = 2
	}
}
