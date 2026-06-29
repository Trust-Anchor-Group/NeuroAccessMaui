namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Identifies the scan target handled by the OCR service.
	/// </summary>
	public enum OcrScanTargetKind
	{
		/// <summary>
		/// Reads a machine-readable zone.
		/// </summary>
		Mrz = 0,

		/// <summary>
		/// Reserved QR-code target for future workflows.
		/// </summary>
		QrCode = 1
	}
}
