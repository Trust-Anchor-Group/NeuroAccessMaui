namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Identifies the scene rendered by <see cref="NfcScanVisual"/>.
	/// </summary>
	public enum NfcScanVisualState
	{
		/// <summary>
		/// Shows a document cover with the chip symbol highlighted.
		/// </summary>
		Intro,

		/// <summary>
		/// Shows the chip symbol with an indeterminate spinner while the scan is prepared.
		/// </summary>
		Preparing,

		/// <summary>
		/// Shows the phone settling onto the document with NFC waves while waiting for the chip.
		/// </summary>
		Searching,

		/// <summary>
		/// Shows the phone resting on the document without motion, ready for the user to start.
		/// </summary>
		Paused,

		/// <summary>
		/// Shows a segmented progress ring around the chip symbol while the chip is read.
		/// </summary>
		Reading,

		/// <summary>
		/// Shows the completed ring resolving into a check mark.
		/// </summary>
		Success,

		/// <summary>
		/// Shows a warning ring and exclamation mark after a failed attempt.
		/// </summary>
		Failure
	}
}
