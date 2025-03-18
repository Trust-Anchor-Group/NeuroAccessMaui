using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // For MainThread

namespace NeuroAccessMaui.UI.MVVM
{
	public enum TaskStatus
	{
		NotStarted,
		Loading,
		Succeeded,
		Faulted,
		Canceled
	}

	/// <summary>
	/// Provides a data-binding friendly mechanism to manage and report the status of asynchronous operations.
	/// </summary>
	/// <typeparam name="TResult">
	/// The type of the result produced by the asynchronous operation. If the operation does not produce a result,
	/// TResult will be <see cref="object"/>.
	/// </typeparam>
	/// <typeparam name="TProgress">
	/// The type used for progress reporting. This allows you to provide progress updates in a strongly-typed manner,
	/// for example, an integer percentage or a custom progress object.
	/// </typeparam>
	/// <remarks>
	/// This class encapsulates asynchronous operation management and exposes a variety of useful properties and methods:
	/// <list type="bullet">
	///   <item>
	///     <description>
	///       <c>Task</c> – Gets the current running task.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       <c>Result</c> – Returns the result of the operation when the task completes successfully.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       <c>State</c> – Indicates the current state of the task (NotStarted, Loading, Succeeded, Faulted, or Canceled).
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       <c>Progress reporting</c> – The <c>Progress</c> property reflects the latest progress value reported by the task.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       <c>Cancellation support</c> – A cancellation token is managed internally to support task cancellation.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       <c>Generation fallback</c> – Implements a generation-based mechanism to ensure that if a new task is started (or refreshed),
	///       any progress or completion from an old, cancelled task is ignored. This prevents stale operations from updating the UI.
	///     </description>
	///   </item>
	/// </list>
	/// <para>
	/// <strong>Usage:</strong> Typically, you use an instance of this notifier as part of your MVVM command infrastructure.
	/// The asynchronous operation’s status can be bound to UI elements (for example, to show loading indicators, display error messages, etc.).
	/// </para>
	/// <para>
	/// <strong>Warning:</strong> If the asynchronous operation has side effects (such as network requests, file operations,
	/// or interactions with external systems), reloading or refreshing may not immediately halt the previous operation. The stale operation
	/// may still complete its execution and perform its side effects. It is therefore important to ensure that such operations are idempotent
	/// or that any unintended effects are properly managed.
	/// </para>
	/// </remarks>
	public partial class ObservableTask<TResult, TProgress> : ObservableObject, IDisposable
	{
		// SemaphoreSlim to protect shared mutable state
		private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
		private bool disposed = false;

		// Backing field for the latest progress value.
		private TProgress progress = default!;

		// Commands to notify when state changes.
		private readonly List<IRelayCommand> notifyCommands = new List<IRelayCommand>();

		// The factory delegate to create tasks; stored so that a new load can be triggered.
		private Func<TaskContext<TProgress>, Task<TResult>>? taskFactory;

		/// A generation counter to identify the currently active task.
		private int currentGeneration = 0;

		/// <summary>
		/// The current task being processed
		/// </summary>
		/// <remarks>
		/// <c>Warning: </c> Will propagate exception and return value
		/// </remarks>
		public Task<TResult>? CurrentTask { get; private set; }

		/// <summary>
		/// The current watcher monitoring the current Task
		/// </summary>
		/// <remarks>
		/// <c>Note: </c> Will not throw exceptions. Completes when CurrentTask is done and UI has been notified
		/// </remarks>
		public Task? CurrentWatcher { get; private set; }

		/// <summary>
		/// Returns the result if the task has completed successfully.
		/// </summary>
		public TResult? Result => (this.CurrentTask is { Status: System.Threading.Tasks.TaskStatus.RanToCompletion })
			 ? this.CurrentTask.Result
			 : default;

		/// <summary>
		/// Gets the current state.
		/// </summary>
		public TaskStatus State
		{
			get
			{
				if (this.CurrentTask is null)
					return TaskStatus.NotStarted;
				if (!this.CurrentTask.IsCompleted)
					return TaskStatus.Loading;
				if (this.CurrentTask.IsCanceled)
					return TaskStatus.Canceled;
				if (this.CurrentTask.IsFaulted)
					return TaskStatus.Faulted;
				return TaskStatus.Succeeded;
			}
		}

		// Convenience properties for data binding.
		public bool IsRefreshing { get; private set; }
		public bool IsNotStarted => this.State == TaskStatus.NotStarted;
		public bool IsStarted => this.State != TaskStatus.NotStarted;
		public bool IsLoading => this.State == TaskStatus.Loading;
		public bool IsNotLoading => this.State != TaskStatus.Loading;
		public bool IsSucceeded => this.State == TaskStatus.Succeeded;
		public bool IsNotSucceeded => this.State != TaskStatus.Succeeded;
		public bool IsFaulted => this.State == TaskStatus.Faulted;
		public bool IsNotFaulted => this.State != TaskStatus.Faulted;
		public bool IsCanceled => this.State == TaskStatus.Canceled;
		public bool IsNotCanceled => this.State != TaskStatus.Canceled;

		/// <summary>
		/// If the task faulted, returns its AggregateException.
		/// </summary>
		public AggregateException? Exception => this.CurrentTask?.Exception;

		/// <summary>
		/// The inner exception, if available.
		/// </summary>
		public Exception? InnerException => this.Exception?.InnerException;

		/// <summary>
		/// A friendly error message from the exception.
		/// </summary>
		public string? ErrorMessage => this.InnerException?.Message ?? this.Exception?.Message ?? string.Empty;

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
		/// Cancels any running task and disposes the CancellationTokenSource.
		/// </summary>
		private void CancelExistingTask()
		{

			if (this.CancellationTokenSource == null)
				return;
			try
			{
				if (this.CurrentTask is { IsCompleted: false })
				{
					this.CancellationTokenSource.Cancel();
				}
			}
			catch
			{
				// Ignored.
			}
			finally
			{
				this.CancellationTokenSource.Dispose();
				this.CancellationTokenSource = null;
			}
		}

		/// <summary>
		/// Starts a new task using the stored factory.
		/// If a task is already running, it is canceled first.
		/// The parameter isRefresh is passed to the factory to indicate whether this is a refresh.
		/// </summary>
		private void StartNewTask(bool isRefresh)
		{
			this.semaphore.Wait();
			try
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

				// Create a progress reporter that ensures UI updates.
				Progress<TProgress> ProgressReporter = new Progress<TProgress>(value =>
				{
					if (ThisGeneration == this.currentGeneration)
					{
						MainThread.BeginInvokeOnMainThread(() => this.Progress = value);
					}
				});

				TaskContext<TProgress> Context = new(isRefresh, this.CancellationTokenSource.Token, ProgressReporter);
				// Start the new task.
				this.CurrentTask = this.taskFactory.Invoke(Context);
				this.CurrentWatcher = this.WatchTaskAsync(ThisGeneration);
			}
			finally
			{
				this.semaphore.Release();
			}
			// Notify bindings and start watching the new task.
			this.NotifyAll();
		}

		/// <summary>
		/// Monitors the current task and notifies property changes when it completes.
		/// </summary>
		private async Task WatchTaskAsync(int Generation)
		{
			try
			{
				if (this.CurrentTask != null)
				{
					await this.CurrentTask;
				}
			}
			catch
			{
				// Exceptions are surfaced via Exception/InnerException/ErrorMessage.
			}
			finally
			{
				if (Generation == this.currentGeneration)
				{
					if(this.IsRefreshing)
						this.IsRefreshing = false;
					this.NotifyAll();
				}
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
				this.OnPropertyChanged(nameof(this.IsNotStarted));
				this.OnPropertyChanged(nameof(this.IsStarted));
				this.OnPropertyChanged(nameof(this.IsLoading));
				this.OnPropertyChanged(nameof(this.IsNotLoading));
				this.OnPropertyChanged(nameof(this.IsSucceeded));
				this.OnPropertyChanged(nameof(this.IsNotSucceeded));
				this.OnPropertyChanged(nameof(this.IsFaulted));
				this.OnPropertyChanged(nameof(this.IsNotFaulted));
				this.OnPropertyChanged(nameof(this.IsCanceled));
				this.OnPropertyChanged(nameof(this.IsNotCanceled));
				this.OnPropertyChanged(nameof(this.Result));
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
				this.RefreshCommand.NotifyCanExecuteChanged();
			});
		}

		/// <summary>
		/// Loads (or reloads) the task using the provided factory.
		/// If a task is already in progress, it is canceled and replaced.
		/// This method supports repeated loading.
		/// </summary>
		/// <param name="TaskFactory">
		/// The factory delegate used to create the task.
		/// The TaskContext parameter contains whether this is a refresh, a cancellation token, and a progress reporter.
		/// </param>
		/// <param name="NotifyCommands">
		/// Optional RelayCommands that should be notified when the state changes.
		/// </param>
		public void Load(Func<TaskContext<TProgress>, Task<TResult>> TaskFactory, params IRelayCommand[] NotifyCommands)
		{
			this.semaphore.Wait();
			try
			{
				this.taskFactory = TaskFactory;
				this.notifyCommands.Clear();
				this.notifyCommands.AddRange(NotifyCommands);
			}
			finally
			{
				this.semaphore.Release();
			}

			// For initial load, pass false.
			this.StartNewTask(isRefresh: false);
		}

		/// <summary>
		/// Asynchronously waits for the current watcher task to complete.
		/// </summary>
		/// <remarks>
		/// If a watcher is active, this method awaits its completion; if no watcher exists,
		/// the method returns immediately. This ensures that any UI updates or notifications
		/// associated with the current task have finished processing.
		/// </remarks>
		/// <returns>A task that represents the asynchronous wait operation.</returns>
		public async Task WaitCurrentAsync()
		{
			Task? Watcher = this.CurrentWatcher;
			if (Watcher is null)
				return;
			await Watcher;
		}

		/// <summary>
		/// Asynchronously waits until all UI notifications and state updates for the current task have been processed.
		/// </summary>
		/// <remarks>
		/// This method repeatedly awaits the current watcher until it remains unchanged,
		/// meaning that no new task has been started in the interim. Use this method when you need
		/// to ensure that all notifications (triggered by task completion, cancellation, or refresh)
		/// have been fully handled before proceeding.
		/// </remarks>
		/// <returns>A task that completes once the latest watcher task has finished and no new watcher has replaced it.</returns>
		public async Task WaitAllAsync()
		{
			while (true)
			{
				Task? Watcher = this.CurrentWatcher;
				if (Watcher is null)
					return;
				await Watcher;

				Task? NewWatcher = this.CurrentWatcher;
				if (NewWatcher is not null && Watcher != NewWatcher)
					continue;
				break;
			}
		}

		/// <summary>
		/// Reload the current task
		/// This cancels any running task and starts a new one using the stored factory.
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsStarted))]
		public void Reload()
		{
			this.StartNewTask(isRefresh: false);
		}

		/// <summary>
		/// Refreshes the current task.
		/// This cancels any running task and starts a new one using the stored factory with the refresh flag.
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsStarted))]
		public void Refresh()
		{
			this.semaphore.Wait();
			try
			{
				this.IsRefreshing = true;
			}
			finally
			{
				this.semaphore.Release();
			}

			// For refresh, pass true.
			this.StartNewTask(isRefresh: true);
		}

		/// <summary>
		/// Cancels the running task.
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsNotCanceled))]
		public void Cancel()
		{
			this.semaphore.Wait();
			try
			{
				this.CancellationTokenSource?.Cancel();
			}
			catch (Exception)
			{
				// Ignored.
			}
			finally
			{
				this.semaphore.Release();
			}
		}

		/// <summary>
		/// Disposes the notifier and its resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Protected dispose pattern implementation.
		/// </summary>
		/// <param name="disposing">True if called from Dispose.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				// Dispose managed resources.
				this.semaphore.Dispose();
				if (this.CancellationTokenSource != null)
				{
					try
					{
						this.CancellationTokenSource.Cancel();
					}
					catch
					{
						// ignored
					}

					this.CancellationTokenSource.Dispose();
				}
			}

			this.disposed = true;
		}
	}
}
