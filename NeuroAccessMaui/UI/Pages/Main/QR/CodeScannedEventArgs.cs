namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>
	/// An event args class that holds dat about the currently scanned QR code.
	/// </summary>
	public sealed class CodeScannedEventArgs(string Url) : EventArgs
	{
		/// <summary>
		/// The scanned QR code URL
		/// </summary>
		public string Url { get; } = Url;
	}
}
