namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Describes the next user-facing scanner guidance state for MRZ capture.
	/// </summary>
	public enum MrzCaptureGuidanceState
	{
		/// <summary>
		/// The scanner is looking for the document page.
		/// </summary>
		FindPage = 0,

		/// <summary>
		/// The scanner needs all document corners in view.
		/// </summary>
		FitAllCorners = 1,

		/// <summary>
		/// The scanner detected glare that should be reduced.
		/// </summary>
		ReduceGlare = 2,

		/// <summary>
		/// The document should be moved closer to the camera.
		/// </summary>
		MoveCloser = 3,

		/// <summary>
		/// The frame is usable and the user should hold still.
		/// </summary>
		HoldSteady = 4,

		/// <summary>
		/// A commit capture is in progress.
		/// </summary>
		Capturing = 5,

		/// <summary>
		/// The scan has reached review or validation.
		/// </summary>
		Review = 6,

		/// <summary>
		/// Automatic capture is unlikely to succeed and manual fallback should be offered.
		/// </summary>
		ManualFallback = 7
	}
}
