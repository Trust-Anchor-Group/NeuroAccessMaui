

namespace NeuroAccessMaui.UI.MVVM
{
	/// <summary>
	/// Represents the context for an asynchronous task execution.
	/// </summary>
	/// <typeparam name="TProgress">The type used for progress reporting.</typeparam>
	public class TaskContext<TProgress>(
		bool IsRefreshing,
		CancellationToken CancellationToken,
		IProgress<TProgress> Progress)
	{
		/// <summary>
		/// Indicates whether the task is being refreshed.
		/// </summary>
		public bool IsRefreshing { get; } = IsRefreshing;

		/// <summary>
		/// The cancellation token for the task.
		/// </summary>
		public CancellationToken CancellationToken { get; } = CancellationToken;

		/// <summary>
		/// If cancellation has been requested
		/// </summary>
		public bool IsCanceled => this.CancellationToken.IsCancellationRequested;

		/// <summary>
		/// The progress reporter for the task.
		/// </summary>
		public IProgress<TProgress> Progress { get; } = Progress;
	}
}
