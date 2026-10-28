using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// An extensions class for the <see cref="Task"/> class.
	/// </summary>
	public static class TaskExtensions
	{
		/// <summary>
		/// Helper method to wait for a task to complete, but with a given time limit.
		/// </summary>
		/// <typeparam name="TResult">The task's result.</typeparam>
		/// <param name="OperationTask">The task to await.</param>
		/// <param name="Timeout">The maximum time to wait for the task.</param>
		/// <returns>Task waiting for the original task to complete, or a timeout to occur.</returns>
		public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> OperationTask, TimeSpan Timeout)
		{
			using CancellationTokenSource DelayCancellation = new();
			Task CompletedTask = await Task.WhenAny(OperationTask, Task.Delay(Timeout, DelayCancellation.Token));
			if (CompletedTask == OperationTask)
			{
				DelayCancellation.Cancel();
				return await OperationTask;  // Very important in order to propagate exceptions
			}

			// Observe a late fault on the abandoned task, so it is logged instead of surfacing later as an unobserved
			// task exception.
			_ = OperationTask.ContinueWith(FaultedTask =>
			{
				AggregateException? Exception = FaultedTask.Exception;
				if (Exception is not null)
					ServiceRef.LogService.LogException(Exception);
			}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			throw new TimeoutException();
		}
	}
}
