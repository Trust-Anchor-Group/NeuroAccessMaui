namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>Describes camera and scan feedback without changing QR result handling.</summary>
	public enum QrScannerState
	{
		/// <summary>The camera is starting.</summary>
		Starting,
		/// <summary>The scanner is ready for input.</summary>
		Scanning,
		/// <summary>The current value passed the existing scanner checks.</summary>
		Accepted,
		/// <summary>The current value cannot be scanned here.</summary>
		Rejected,
		/// <summary>The camera could not provide frames.</summary>
		CameraUnavailable,
		/// <summary>The scanner is returning to its caller.</summary>
		Closing
	}
}
