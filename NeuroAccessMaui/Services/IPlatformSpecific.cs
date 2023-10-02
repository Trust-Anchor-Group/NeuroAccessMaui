namespace NeuroAccessMaui.Services;

public interface IPlatformSpecific
{
	/// <summary>
	/// If screen capture prohibition is supported
	/// </summary>
	public bool CanProhibitScreenCapture { get; }

	/// <summary>
	/// If screen capture is prohibited or not.
	/// </summary>
	public bool ProhibitScreenCapture { get; set; }

	/// <summary>
	/// Gets the ID of the device
	/// </summary>
	string? GetDeviceId();

	/// <summary>
	/// Closes the application
	/// </summary>
	Task CloseApplication();

	/// <summary>
	/// Make a blurred screenshot
	/// </summary>
	Task<byte[]> CaptureScreen(int blurRadius);
}
