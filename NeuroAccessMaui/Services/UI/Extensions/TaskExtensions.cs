namespace NeuroAccessMaui.Services.UI.Extensions;

public static class TaskExtensions
{
	public static async void FireAndForget(this Task Task, Action<Exception>? OnException = null)
	{
		try
		{
			await Task;
		}
		catch (Exception ex)
		{
			OnException?.Invoke(ex);
		}
	}
}
