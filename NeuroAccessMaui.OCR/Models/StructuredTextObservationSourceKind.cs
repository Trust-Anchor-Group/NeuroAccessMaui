namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Identifies how a structured-text OCR observation was produced.
	/// </summary>
	public enum StructuredTextObservationSourceKind
	{
		/// <summary>
		/// The observation came from one OCR pass over the whole localized text block.
		/// </summary>
		Block = 0,

		/// <summary>
		/// The observation came from one OCR pass over an individual localized text line.
		/// </summary>
		Line = 1
	}
}
