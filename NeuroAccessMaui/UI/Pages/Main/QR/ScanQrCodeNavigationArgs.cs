using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Main.QR;

/// <summary>
/// Holds navigation parameters specific to views scanning a QR code.
/// </summary>
public class ScanQrCodeNavigationArgs(string? CommandName = null, string? AllowedIcon = null) : NavigationArgs
{
	/// <summary>
	/// Creates an instance of the <see cref="ScanQrCodeNavigationArgs"/> class.
	/// </summary>
	public ScanQrCodeNavigationArgs() : this(null, null) { }

	/// <summary>
	/// The allowed icon to display.
	/// </summary>
	public string? AllowedIcon { get; } = AllowedIcon;

	/// <summary>
	/// The command name (localized) to display.
	/// </summary>
	public string? CommandName { get; } = CommandName;

	/// <summary>
	/// Task completion source; can be used to wait for a result.
	/// </summary>
	public TaskCompletionSource<string?> QrCodeScanned { get; internal set; } = new();
}
