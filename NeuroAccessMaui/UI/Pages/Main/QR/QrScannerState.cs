namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>Describes camera, preparation, and scan feedback states of the scanner.</summary>
	public enum QrScannerState
	{
		/// <summary>The camera is starting.</summary>
		Starting,
		/// <summary>The scanner is ready for input.</summary>
		Scanning,
		/// <summary>The current value passed the scanner checks and is being prepared for its caller.</summary>
		Validating,
		/// <summary>The current value is prepared and is being returned to the caller.</summary>
		Ready,
		/// <summary>The current value cannot be used here, or is invalid.</summary>
		Rejected,
		/// <summary>The current value could not be prepared because of a temporary failure; it can be retried.</summary>
		Unavailable,
		/// <summary>The camera could not provide frames.</summary>
		CameraUnavailable,
		/// <summary>The scanner is returning to its caller.</summary>
		Closing
	}
}
