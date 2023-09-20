namespace NeuroAccessMaui.Extensions;

/// <summary>
/// An extensions class for the <see cref="Task"/> class.
/// </summary>
public static class TaskExtensions
{
	/// <summary>
	/// Helper method to wait for a task to complete, but with a given time limit.
	/// </summary>
	/// <typeparam name="TResult">The task's result.</typeparam>
	/// <param name="task">The task to await.</param>
	/// <param name="timeout">The maximum time to wait for the task.</param>
	/// <returns>Task waiting for the original task to complete, or a timeout to occur.</returns>
	public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
	{
		using CancellationTokenSource tcs = new();
		Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, tcs.Token));
		if (completedTask == task)
		{
			tcs.Cancel();
			return await task;  // Very important in order to propagate exceptions
		}
		throw new TimeoutException();
	}
}
