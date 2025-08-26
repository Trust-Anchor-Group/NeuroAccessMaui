using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services; // For MainThread
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM
{
	/// <summary>
	/// UI-friendly status for an observable async operation.
	/// (Renamed to avoid collision with System.Threading.Tasks.TaskStatus.)
	/// </summary>
	public enum ObservableTaskStatus
	{
		Pending,
		Running,
		Succeeded,
		Failed,
		Canceled
	}

	/// <summary>
	/// Provides a data-binding friendly mechanism to manage and report the status of asynchronous operations.
	/// </summary>
	/// <typeparam name="TProgress">
	/// The type used for progress reporting. This allows you to provide progress updates in a strongly-typed manner,
	/// for example, an integer percentage or a custom progress object.
	/// </typeparam>
	/// <remarks>
	/// This class encapsulates asynchronous operation management and exposes a variety of useful properties and methods:
	/// <list type="bullet">
	///   <item><description><c>CurrentTask</c> – The current running task (propagates exceptions if awaited directly).</description></item>
	///   <item><description><c>CurrentWatcher</c> – Internal watcher that completes after UI notifications.</description></item>
	///   <item><description><c>State</c> – Indicates current state (Pending, Running, Succeeded, Failed, or Canceled).</description></item>
	///   <item><description><c>Progress</c> – Reflects latest progress value reported by the task.</description></item>
	///   <item><description><c>Cancellation</c> – A token source is managed internally to support cancellation.</description></item>
	///   <item><description><c>Generation fallback</c> – Newer tasks supersede older ones; stale updates are ignored.</description></item>
	/// </list>
	/// <para><strong>Warning:</strong> If the operation has side effects, ensure idempotency or manage unintended effects when canceling/reloading.</para>
	/// </remarks>
	public partial class ObservableTask<TProgress> : ObservableObject, IDisposable
	{
		private readonly object syncObject = new();

		// Backing field for the latest progress value.
		private TProgress progress = default!;

		// Commands to notify when state changes.
		private readonly List<IRelayCommand> notifyCommands = new();

		// The factory delegate to create tasks; stored so that a new load can be triggered.
		private Func<TaskContext<TProgress>, Task>? taskFactory;

		// A generation counter to identify the currently active task.
		private int currentGeneration = 0;

		/// <summary>
		/// If true, invokes the task factory via Task.Run to force background-thread execution (useful for CPU-bound work).
		/// Default is false (good for async/IO-bound code).
		/// </summary>
		public bool UseTaskRun { get; set; } = false;

		/// <summary>
		/// The current task being processed.
		/// </summary>
		/// <remarks>
		/// <c>Warning:</c> Awaiting this will propagate exceptions.
		/// </remarks>
		public Task? CurrentTask { get; private set; }

		/// <summary>
		/// The current watcher monitoring the current Task.
		/// </summary>
		/// <remarks>
		/// Does not propagate exceptions. Completes when CurrentTask is done and UI has been notified.
		/// </remarks>
		public Task? CurrentWatcher { get; private set; }

		/// <summary>
		/// Gets the current state.
		/// </summary>
		public ObservableTaskStatus State { get; private set; }

		// Convenience properties for data binding.
		public bool IsPending => this.State == ObservableTaskStatus.Pending;
		public bool IsNotPending => this.State != ObservableTaskStatus.Pending;
		public bool IsRunning => this.State == ObservableTaskStatus.Running;
		public bool IsNotRunning => this.State != ObservableTaskStatus.Running;
		public bool IsSucceeded => this.State == ObservableTaskStatus.Succeeded;
		public bool IsNotSucceeded => this.State != ObservableTaskStatus.Succeeded;
		public bool IsFailed => this.State == ObservableTaskStatus.Failed;
		public bool IsNotFailed => this.State != ObservableTaskStatus.Failed;
		public bool IsCanceled => this.State == ObservableTaskStatus.Canceled;
		public bool IsNotCanceled => this.State != ObservableTaskStatus.Canceled;
		public bool IsRefreshing { get; private set; }

		/// <summary>
		/// If the task faulted, returns its AggregateException (if applicable).
		/// </summary>
		public AggregateException? Exception { get; private set; }

		/// <summary>
		/// The inner exception, if available.
		/// </summary>
		public Exception? InnerException { get; private set; }

		/// <summary>
		/// A friendly error message from the exception.
		/// </summary>
		public string? ErrorMessage { get; private set; }

		/// <summary>
		/// The latest progress value reported by the task.
		/// </summary>
		public TProgress Progress
		{
			get => this.progress;
			private set
			{
				if (!EqualityComparer<TProgress>.Default.Equals(this.progress, value))
				{
					this.progress = value;
					// Always update on the UI thread.
					MainThread.BeginInvokeOnMainThread(() => this.OnPropertyChanged(nameof(this.Progress)));
				}
			}
		}

		/// <summary>
		/// The CancellationTokenSource for the running task.
		/// </summary>
		public CancellationTokenSource? CancellationTokenSource { get; private set; }

		/// <summary>
		/// Optional event fired when <see cref="State"/> changes. Handy for telemetry or local reactions.
		/// </summary>
		public event EventHandler<ObservableTaskStatus>? StateChanged;

		/// <summary>
		/// Cancels any running task.
		/// </summary>
		private void CancelExistingTask()
		{
			CancellationTokenSource? Cts = this.CancellationTokenSource;
			if (Cts is null)
				return;

			try
			{
				if (this.CurrentTask is { IsCompleted: false })
				{
					Cts.Cancel();
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Starts a new task using the stored factory.
		/// If a task is already running, it is canceled first.
		/// </summary>
		private void StartNewTask(bool isRefresh)
		{
			try
			{
				lock (this.syncObject)
				{
					if (this.taskFactory is null)
						return;

					// Increment the generation token.
					int ThisGeneration = ++this.currentGeneration;

					// Cancel any existing task.
					this.CancelExistingTask();

					this.CancellationTokenSource = new CancellationTokenSource();
					this.CurrentTask = null;
					this.CurrentWatcher = null;
					this.ErrorMessage = string.Empty;
					this.Exception = null;
					this.InnerException = null;
					this.IsRefreshing = isRefresh;
					this.State = ObservableTaskStatus.Running;

					// Create a progress reporter that ensures UI updates and ignores stale generations.
					Progress<TProgress> ProgressReporter = new(value =>
					{
						if (ThisGeneration == this.currentGeneration)
						{
							MainThread.BeginInvokeOnMainThread(() => this.Progress = value);
						}
					});

					TaskContext<TProgress> Context = new(isRefresh, this.CancellationTokenSource.Token, ProgressReporter);

					// Start the new task.
					if (this.UseTaskRun)
					{

						// Force background-thread execution (good for CPU-bound work).
						this.CurrentTask = Task.Run(async () => await this.taskFactory!(Context),this.CancellationTokenSource.Token);
					}
					else
					{
						// Default path (good for naturally async/IO-bound work).
						this.CurrentTask = this.taskFactory.Invoke(Context);
					}

					this.CurrentWatcher = this.WatchTaskAsync(ThisGeneration, this.CurrentTask, this.CancellationTokenSource);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				// Notify bindings and start watching the new task.
				this.NotifyAll();
			}
		}

		/// <summary>
		/// Monitors the current task and notifies property changes when it completes.
		/// </summary>
		private async Task WatchTaskAsync(int generation, Task? current, CancellationTokenSource cts)
		{
			// TODO: Add logging for tasks gone rogue if desired.

			AggregateException? AggregateException = null;
			Exception? InnerException = null;
			string? ErrorMessage = null;

			try
			{
				if (current is not null)
				{
					await current.ConfigureAwait(false);
				}
			}
			catch (TaskCanceledException)
			{
				// Normal operation on cancel.
			}
			catch (Exception ex)
			{
				// Exceptions are surfaced via Exception/InnerException/ErrorMessage.
				AggregateException = ex as AggregateException;
				InnerException = ex.InnerException ?? ex;
				ErrorMessage = InnerException.Message;

				// Log by default (can be routed to telemetry later).
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				// Marshal back to UI thread for state updates.
				MainThread.BeginInvokeOnMainThread(() =>
				{
					lock (this.syncObject)
					{
						cts.Dispose();

						if (generation != this.currentGeneration)
							return;

						if (current is null)
							this.State = ObservableTaskStatus.Pending;
						else if (!current.IsCompleted)
							this.State = ObservableTaskStatus.Running;
						else if (current.IsCanceled)
							this.State = ObservableTaskStatus.Canceled;
						else if (current.IsFaulted)
							this.State = ObservableTaskStatus.Failed;
						else
							this.State = ObservableTaskStatus.Succeeded;

						this.Exception = AggregateException;
						this.InnerException = InnerException;
						this.ErrorMessage = ErrorMessage;

						this.IsRefreshing = false;

						if (this.CancellationTokenSource is not null)
						{
							this.CancellationTokenSource = null;
						}
					}

					this.NotifyAll();
					this.StateChanged?.Invoke(this, this.State);
				});
			}
		}

		/// <summary>
		/// Notifies all bound properties and command states on the main thread.
		/// </summary>
		private void NotifyAll()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.State));
				this.OnPropertyChanged(nameof(this.IsPending));
				this.OnPropertyChanged(nameof(this.IsNotPending));
				this.OnPropertyChanged(nameof(this.IsRunning));
				this.OnPropertyChanged(nameof(this.IsNotRunning));
				this.OnPropertyChanged(nameof(this.IsSucceeded));
				this.OnPropertyChanged(nameof(this.IsNotSucceeded));
				this.OnPropertyChanged(nameof(this.IsFailed));
				this.OnPropertyChanged(nameof(this.IsNotFailed));
				this.OnPropertyChanged(nameof(this.IsCanceled));
				this.OnPropertyChanged(nameof(this.IsNotCanceled));
				this.OnPropertyChanged(nameof(this.Exception));
				this.OnPropertyChanged(nameof(this.InnerException));
				this.OnPropertyChanged(nameof(this.ErrorMessage));
				this.OnPropertyChanged(nameof(this.Progress));
				this.OnPropertyChanged(nameof(this.IsRefreshing));

				foreach (IRelayCommand Command in this.notifyCommands)
				{
					Command.NotifyCanExecuteChanged();
				}

				// Also update the automatically generated commands.
				this.CancelCommand.NotifyCanExecuteChanged();
				this.ReloadCommand.NotifyCanExecuteChanged();
				this.RefreshCommand.NotifyCanExecuteChanged();
			});
		}

		/// <summary>
		/// Asynchronously waits for the current watcher task to complete.
		/// </summary>
		public async Task WaitCurrentAsync()
		{
			Task? Watcher = this.CurrentWatcher;
			if (Watcher is null)
				return;

			await Watcher.ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously waits until all UI notifications and state updates for the current task have been processed.
		/// </summary>
		public async Task WaitAllAsync(bool waitWhilePending = false)
		{
			while (true)
			{
				Task? Watcher = this.CurrentWatcher;
				if (Watcher is null)
				{
					if (waitWhilePending && this.State == ObservableTaskStatus.Pending)
					{
						await Task.Delay(Constants.Delays.Default).ConfigureAwait(false);
						continue;
					}

					return;
				}

				await Watcher.ConfigureAwait(false);

				Task? NewWatcher = this.CurrentWatcher;
				if (NewWatcher is not null && !ReferenceEquals(Watcher, NewWatcher))
					continue;

				break;
			}
		}

		/// <summary>
		/// Store the factory and optional commands without starting the task.
		/// Use <see cref="Run"/> or <see cref="RunRefresh"/> to start later.
		/// </summary>
		public void Configure(Func<TaskContext<TProgress>, Task> task, params IRelayCommand[] notifyCommands)
		{
			lock (this.syncObject)
			{
				this.taskFactory = task;
				this.notifyCommands.Clear();
				if (notifyCommands.Length > 0)
					this.notifyCommands.AddRange(notifyCommands);
			}

			// Leave state as-is (likely Pending). No auto-start here.
			this.NotifyAll();
		}

		/// <summary>
		/// Loads (configures) and immediately starts the task (back-compat convenience).
		/// </summary>
		public void Load(Func<TaskContext<TProgress>, Task> task, params IRelayCommand[] notifyCommands)
		{
			this.Configure(task, notifyCommands);
			this.StartNewTask(isRefresh: false);
		}

		public void Load(Func<Task> task, params IRelayCommand[] notifyCommands)
		{
			// Wrap the parameterless task factory in a lambda that ignores the TaskContext.
			this.Load(_ => task(), notifyCommands);
		}

		/// <summary>
		/// Start the configured task now (no CanExecute gating).
		/// </summary>
		public void Run() => this.StartNewTask(isRefresh: false);

		/// <summary>
		/// Start the configured task now with the refresh flag (no CanExecute gating).
		/// </summary>
		public void RunRefresh() => this.StartNewTask(isRefresh: true);

		/// <summary>
		/// Reload the current task.
		/// This cancels any running task and starts a new one using the stored factory.
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsNotPending))]
		public void Reload()
		{
			if (this.IsPending)
				return;

			this.StartNewTask(isRefresh: false);
		}

		/// <summary>
		/// Refreshes the current task.
		/// This cancels any running task and starts a new one using the stored factory with the refresh flag.
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsNotPending))]
		public void Refresh()
		{
			if (this.IsPending)
				return;

			this.StartNewTask(isRefresh: true);
		}

		/// <summary>
		/// Cancels the running task.
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsRunning))]
		public void Cancel()
		{
			if (!this.IsRunning)
				return;

			lock (this.syncObject)
			{
				try
				{
					this.CancellationTokenSource?.Cancel();
				}
				catch
				{
					// Ignored.
				}
			}
		}
		private bool disposed;
		// Public Dispose: callers clean up managed + unmanaged
		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		// Finalizer only if you manage native resources.
		~ObservableTask()
		{
			this.Dispose(disposing: false);
		}

		// The core pattern
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;
			this.disposed = true;

			if (disposing)
			{
				lock (this.syncObject)
				{
					try { this.CancellationTokenSource?.Cancel(); }
					catch { /* ignore */ }

					this.CancellationTokenSource?.Dispose();
					this.CancellationTokenSource = null;
				}
			}
		}

	}

	public partial class ObservableTask : ObservableTask<int>
	{
	}

	public sealed class ObservableTask<TResult, TProgress> : ObservableTask<TProgress>
	{
		public TResult? Result { get; private set; }

		public void Load(Func<TaskContext<TProgress>, Task<TResult>> op, params IRelayCommand[] notify)
		{
			base.Load(async ctx =>
			{
				this.Result = await op(ctx);
			}, notify);
		}
	}

}
