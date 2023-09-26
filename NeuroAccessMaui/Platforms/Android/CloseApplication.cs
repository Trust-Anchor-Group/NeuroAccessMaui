using Android.App;

namespace NeuroAccessMaui.DeviceSpecific;

public class CloseApplication : ICloseApplication
{
	/// <summary>
	/// Closes the application
	/// </summary>
	public Task Close()
	{
		Activity? Activity = Android.App.Application.Context as Activity;    // TODO: returns null. Context points to Application instance.
		Activity?.FinishAffinity();

		Java.Lang.JavaSystem.Exit(0);

		return Task.CompletedTask;
	}
}
