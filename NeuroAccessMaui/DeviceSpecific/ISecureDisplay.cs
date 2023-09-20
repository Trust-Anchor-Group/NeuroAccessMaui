namespace NeuroAccessMaui.DeviceSpecific;

/// <summary>
/// Dependency interface for device-specific securing of display.
/// </summary>
public interface ISecureDisplay
{
	/// <summary>
	/// If screen capture is prohibited or not.
	/// </summary>
	public bool ProhibitScreenCapture { get; set; }
}
