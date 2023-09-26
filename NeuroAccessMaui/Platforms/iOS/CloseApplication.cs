namespace NeuroAccessMaui.DeviceSpecific;

public class CloseApplication : ICloseApplication
{
	/// <summary>
	/// Closes the application
	/// </summary>
	public Task Close()
	{
		Environment.Exit(0);
		return Task.CompletedTask;
	}
}
