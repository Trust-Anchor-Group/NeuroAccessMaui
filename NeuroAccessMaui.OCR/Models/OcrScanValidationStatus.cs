namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Describes the final validation outcome of a scan attempt.
	/// </summary>
	public enum OcrScanValidationStatus
	{
		/// <summary>
		/// The scan produced a strictly valid result.
		/// </summary>
		Succeeded = 0,

		/// <summary>
		/// OCR ran, but the target content was invalid or incomplete.
		/// </summary>
		Invalid = 1,

		/// <summary>
		/// The requested target is not implemented.
		/// </summary>
		UnsupportedTarget = 2,

		/// <summary>
		/// The scan failed before validation could complete.
		/// </summary>
		Failed = 3
	}
}
