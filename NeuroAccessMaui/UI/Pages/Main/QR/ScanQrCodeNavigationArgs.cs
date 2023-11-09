using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Main.QR;

/// <summary>
/// Holds navigation parameters specific to views scanning a QR code.
/// </summary>
public class ScanQrCodeNavigationArgs(string? QrTitle = null, string? AllowedSchema = null) : NavigationArgs
{
	/// <summary>
	/// Creates an instance of the <see cref="ScanQrCodeNavigationArgs"/> class.
	/// </summary>
	public ScanQrCodeNavigationArgs() : this(null) { }

	/// <summary>
	/// The page title.
	/// </summary>
	public string? QrTitle { get; } = QrTitle;

	/// <summary>
	/// The allowed schema to display.
	/// </summary>
	public string? AllowedSchema { get; } = AllowedSchema;

	/// <summary>
	/// Task completion source; can be used to wait for a result.
	/// </summary>
	public TaskCompletionSource<string?> QrCodeScanned { get; internal set; } = new();
}
