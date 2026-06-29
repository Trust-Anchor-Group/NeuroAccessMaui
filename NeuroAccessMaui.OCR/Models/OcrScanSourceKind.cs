namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Identifies the source mode used by a scan request.
	/// </summary>
	public enum OcrScanSourceKind
	{
		/// <summary>
		/// The request uses a still image.
		/// </summary>
		StillImage = 0,

		/// <summary>
		/// The request uses a live preview frame.
		/// </summary>
		PreviewFrame = 1
	}
}
