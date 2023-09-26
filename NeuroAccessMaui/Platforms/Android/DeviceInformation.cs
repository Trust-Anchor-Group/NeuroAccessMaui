using Android.Provider;

namespace NeuroAccessMaui.DeviceSpecific;

public class DeviceInformation : IDeviceInformation
{
	/// <summary>
	/// Gets the ID of the device.
	/// </summary>
	public string? GetDeviceId()
	{
		Android.Content.ContentResolver? ContentResolver = Android.App.Application.Context.ContentResolver;

		if (ContentResolver is not null)
		{
			return Settings.Secure.GetString(ContentResolver, Settings.Secure.AndroidId);
		}

		return null;
	}
}
