namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Describes how a document-line scanner session ended.
	/// </summary>
	public enum KycDocumentMrzScannerOutcome
	{
		/// <summary>
		/// The scanner has not finished yet.
		/// </summary>
		Pending,

		/// <summary>
		/// The scanner read and accepted the document lines.
		/// </summary>
		Accepted,

		/// <summary>
		/// The user left the scanner without accepting a read.
		/// </summary>
		Cancelled,

		/// <summary>
		/// The scanner could not start because camera access was not granted.
		/// </summary>
		CameraPermissionDenied,

		/// <summary>
		/// The scanner could not start or keep the camera running.
		/// </summary>
		CameraUnavailable,

		/// <summary>
		/// The scanner stopped because of an unexpected error.
		/// </summary>
		Failed
	}
}
