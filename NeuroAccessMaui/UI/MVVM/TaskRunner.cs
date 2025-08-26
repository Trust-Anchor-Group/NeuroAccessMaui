using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.MVVM
{
	/// <summary>
	/// Core, non-UI class that manages the lifecycle of an asynchronous operation:
	/// start, cancellation, progress updates, and final status reporting.
	/// Allows an optional exception handler to override the default logging behavior.
	/// </summary>
	/// <typeparam name="TProgress">Type of progress update values.</typeparam>
	public class TaskRunner<TProgress> : IDisposable
	{
		private readonly object sync = new();
		private Func<TaskContext<TProgress>, Task>? taskFactory;
		private readonly Action<Exception> exceptionHandler;
		private CancellationTokenSource? cts;
		/// A generation counter to identify the currently active task.
		private int currentGeneration = 0;

		/// <summary>
		/// Default ctor
		/// </summary>
		public TaskRunner(Action<Exception>? ExceptionHandler = null)
		{
			this.exceptionHandler = ExceptionHandler ?? (ex => ServiceRef.LogService.LogException(ex));
		}

		/// <summary>
		/// Ctor with task factory injection and optional autostart.
		/// </summary>
		/// <param name="TaskFactory">Factory delegate to create the task.</param>
		/// <param name="AutoStart">If true, starts immediately.</param>
		/// <param name="ExceptionHandler">Custom exception handler.</param>
		public TaskRunner(
			Func<TaskContext<TProgress>, Task> TaskFactory,
			bool AutoStart = true,
			Action<Exception>? ExceptionHandler = null)
			: this(ExceptionHandler)
		{
			this.taskFactory = TaskFactory;
			if (AutoStart)
				this.Start();
		}

		public TaskRunner(
			Func<Task> TaskFactory,
			bool AutoStart = true,
			Action<Exception>? ExceptionHandler = null)
			: this(ExceptionHandler)
		{
			this.taskFactory = async (_) => await TaskFactory();
			if (AutoStart)
				this.Start();
		}

		/// <summary>Current running Task, or null if not started.</summary>
		public Task? CurrentTask { get; private set; }

		/// <summary> Gets a task that completes successfully when <see cref="CurrentTask"/> completes (Succeeded, failed, or canceled). Will never fail </summary>
		public Task? TaskCompleted { get; private set; }

		/// <summary>Latest reported progress value.</summary>
		public TProgress Progress { get; private set; } = default!;

		/// <summary>Current status of the operation.</summary>
		public ObservableTaskStatus Status { get; private set; } = ObservableTaskStatus.Pending;

		/// <summary>Exception thrown, if any.</summary>
		public Exception? Exception { get; private set; }

		/// <summary>Friendly error message from the exception.</summary>
		public string? ErrorMessage { get; private set; }

		/// <summary>The inner exception, if any.</summary>
		public Exception? InnerException { get; private set; }

		/// <summary>Raised whenever Status changes.</summary>
		public event Action? StatusChanged;
		/// <summary>Raised whenever Progress changes.</summary>
		public event Action<TProgress>? ProgressChanged;

		/// <summary>
		/// Start or restart the operation.
		/// </summary>
		/// <param name="TaskFactory">
		/// Delegate receiving (CancellationToken, IProgress&lt;TProgress&gt;) and returning the Task.
		/// </param>
		public void Start(Func<TaskContext<TProgress>, Task> TaskFactory, bool IsRefresh = false)
		{
			lock (this.sync)
			{
				// Increment the generation token.
				int ThisGeneration = ++this.currentGeneration;
				this.taskFactory = TaskFactory;

				// Cancel any existing run
				this.cts?.Cancel();
				this.cts = new CancellationTokenSource();

				Progress<TProgress> Reporter = new Progress<TProgress>(value =>
				{
					if(this.currentGeneration != ThisGeneration)
						return; // Ignore progress updates from ghost tasks.
					this.Progress = value;
					ProgressChanged?.Invoke(value);
				});

				this.Status = ObservableTaskStatus.Running;
				StatusChanged?.Invoke();

				TaskContext<TProgress> Context = new(IsRefresh, this.cts.Token, Reporter);

				this.CurrentTask = TaskFactory(Context);
				this.TaskCompleted = this.WatchTaskAsync(ThisGeneration, this.CurrentTask, this.cts);
			}
		}

		public void Start(Func<Task> TaskFactory)
		{
			this.Start(async (_) => await TaskFactory());
		}

		public void Start()
		{
			if (this.taskFactory is null)
			{
				ServiceRef.LogService.LogWarning("TaskFactory is null, cannot start task.");
				return;
			}
			this.Start(this.taskFactory);
		}

		/// <summary>
		/// Cancels the running operation and disposes the CancellationTokenSource.
		/// </summary>
		public void Cancel()
		{
			lock (this.sync)
			{
				this.cts?.Cancel();
			}
		}

		/// <summary>
		/// Reload (restart) the last configured operation.
		/// </summary>
		/// <remarks>
		/// Restarts the last configured operation, with the IsRefresh flag set to false.
		/// </remarks>
		public void Reload()
		{
			if (this.taskFactory is null)
				return;
			this.Start(this.taskFactory, false);
		}

		/// <summary>
		/// Refresh the last configured operation
		/// </summary>
		/// <remarks>
		/// Restarts the last configured operation, with the IsRefresh flag set to true.
		/// </remarks>
		public void Refresh()
		{
			if (this.taskFactory is null)
				return;
			this.Start(this.taskFactory, true);
		}

		private async Task WatchTaskAsync(int Generation, Task? Current, CancellationTokenSource Cts)
		{
			if (Current is null)
				return;

			try
			{
				await Current;
				lock (this.sync)
				{
					this.Status = ObservableTaskStatus.Succeeded;
				}
			}
			catch (OperationCanceledException) when (Cts.Token.IsCancellationRequested)
			{
				lock (this.sync)
				{
					this.Status = ObservableTaskStatus.Canceled;
				}
			}
			catch (Exception Ex)
			{
				this.exceptionHandler(Ex);
				lock (this.sync)
				{
					this.Status = ObservableTaskStatus.Failed;
					this.Exception = Ex;
					this.InnerException = Ex.InnerException ?? Ex;
					this.ErrorMessage = this.InnerException.Message;
				}
			}
			finally
			{
				Cts.Dispose();
				if (Generation == this.currentGeneration)
					StatusChanged?.Invoke();
			}
		}

		#region IDisposable implementation

		private bool disposed = false;

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.cts?.Dispose();
					this.cts = null;
				}

				this.disposed = true;
			}
		}
		#endregion
	}

	public class TaskRunner : TaskRunner<int>
	{
	}
}
