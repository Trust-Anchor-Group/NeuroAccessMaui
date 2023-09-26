using UIKit;

namespace NeuroAccessMaui.DeviceSpecific;

public class DeviceInformation : IDeviceInformation
{
	/// <summary>
	/// Gets the ID of the device.
	/// </summary>
	public string? GetDeviceId()
	{
		return UIDevice.CurrentDevice.IdentifierForVendor.ToString();
	}
}
