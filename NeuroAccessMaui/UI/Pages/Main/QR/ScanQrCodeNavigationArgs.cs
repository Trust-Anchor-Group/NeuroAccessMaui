using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>
	/// Holds navigation parameters specific to views scanning a QR code.
	/// </summary>
	/// <param name="QrTitle">Title to present</param>
	/// <param name="AllowedSchemas">Schemas permitted.</param>
	public class ScanQrCodeNavigationArgs(string? QrTitle, string[] AllowedSchemas) : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="ScanQrCodeNavigationArgs"/> class.
		/// </summary>
		public ScanQrCodeNavigationArgs() : this(string.Empty, []) { }

		/// <summary>
		/// The page title.
		/// </summary>
		public string? QrTitle { get; } = QrTitle;

		/// <summary>
		/// The allowed schema to display.
		/// </summary>
		public string[] AllowedSchemas { get; } = AllowedSchemas;

		/// <summary>
		/// Task completion source; can be used to wait for a result.
		/// </summary>
		public TaskCompletionSource<string?>? QrCodeScanned { get; internal set; } = new();
	}
}
