namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Describes the intended scanner workload for a request.
	/// </summary>
	public enum OcrScanMode
	{
		/// <summary>
		/// Runs the normal still-image compatibility path.
		/// </summary>
		StillImage = 0,

		/// <summary>
		/// Runs a lightweight preview analysis pass.
		/// </summary>
		PreviewAnalysis = 1,

		/// <summary>
		/// Runs the full commit scan after a preview frame has been accepted.
		/// </summary>
		Commit = 2
	}
}
