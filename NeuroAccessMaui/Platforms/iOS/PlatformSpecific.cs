using UIKit;

namespace NeuroAccessMaui.Services;

public class PlatformSpecific : IPlatformSpecific
{
	public PlatformSpecific()
	{
	}

	/// <inheritdoc/>
	public bool CanProhibitScreenCapture => false;

	/// <inheritdoc/>
	public bool ProhibitScreenCapture // iOS doesn't support screen protection
	{
		get => false;
		set => _ = value; // ignore the value
	}

	/// <inheritdoc/>
	public string? GetDeviceId()
	{
		return UIDevice.CurrentDevice.IdentifierForVendor.ToString();
	}

	/// <inheritdoc/>
	public Task CloseApplication()
	{
		Environment.Exit(0);
		return Task.CompletedTask;
	}
}
