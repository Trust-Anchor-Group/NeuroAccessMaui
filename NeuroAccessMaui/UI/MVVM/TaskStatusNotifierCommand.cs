using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.MVVM
{
	[Flags]
	public enum TaskStatusNotifierCommandOptions
	{
		None = 0,
		AllowConcurrentRestart = 1 << 0,
	}

	public class TaskStatusNotifierCommand<TResult, TProgress> : IAsyncRelayCommand
	{
		private readonly Func<TaskContext<TProgress>, Task<TResult>> taskFactory;
		private readonly Func<bool>? canExecute;
		private readonly TaskStatusNotifierCommandOptions options;

		public TaskStatusNotifier<TResult, TProgress> Notifier { get; }

		// Constructor without canExecute (defaults to always executable and no options)
		public TaskStatusNotifierCommand(
			Func<TaskContext<TProgress>, Task<TResult>> taskFactory)
			 : this(taskFactory, null, TaskStatusNotifierCommandOptions.None)
		{
		}

		// Constructor with optional canExecute and options
		public TaskStatusNotifierCommand(
			Func<TaskContext<TProgress>, Task<TResult>> taskFactory,
			 Func<bool>? canExecute,
			 TaskStatusNotifierCommandOptions options = TaskStatusNotifierCommandOptions.None)
		{
			this.taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
			this.canExecute = canExecute;
			this.options = options;
			this.Notifier = new TaskStatusNotifier<TResult, TProgress>();
			// Forward property changes from the notifier to this command.
			this.Notifier.PropertyChanged += (s, e) => this.PropertyChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Executes the task by starting it through the notifier.
		/// </summary>
		public async Task ExecuteAsync()
		{
			if (!this.CanExecute(null))
				return;

			this.NotifyCanExecuteChanged();

			// Start the task via the notifier.
			this.Notifier.Load(Context => this.taskFactory(Context), this);
			Task<TResult>? Task = this.Notifier.Task;
			if (Task != null)
			{
				// When the task completes, raise CanExecuteChanged (on the UI thread).
				await Task.ContinueWith(
					 _ => this.NotifyCanExecuteChanged(),
					 TaskScheduler.FromCurrentSynchronizationContext());
				this.NotifyCanExecuteChanged();
			}
		}

		/// <summary>
		/// IAsyncRelayCommand overload.
		/// </summary>
		public Task ExecuteAsync(object? parameter) => this.ExecuteAsync();

		/// <summary>
		/// Checks whether the command can execute.
		/// </summary>
		public bool CanExecute(object? parameter)
		{
			bool BaseCanExecute = this.canExecute?.Invoke() ?? true;
			if (!BaseCanExecute)
				return false;

			// If concurrent restarts are not allowed, return false if a task has run.
			if ((this.options & TaskStatusNotifierCommandOptions.AllowConcurrentRestart) == 0)
			{
				if (this.Notifier.IsLoading)
					return false;
			}
			return true;
		}

		public event EventHandler? CanExecuteChanged;
		public void NotifyCanExecuteChanged() => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);

		/// <summary>
		/// ICommand implementation.
		/// </summary>
		public void Execute(object? parameter) => _ = this.ExecuteAsync();

		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Cancels the running task.
		/// </summary>
		public void Cancel() => this.Notifier.Cancel();

		/// <summary>
		/// Exposes the currently executing task.
		/// </summary>
		public Task? ExecutionTask => this.Notifier.Task;

		/// <summary>
		/// Indicates whether the command can be canceled.
		/// </summary>
		public bool CanBeCanceled =>
			 this.Notifier.CancellationTokenSource is not null &&
			 this.Notifier.Task is not null &&
			 !this.Notifier.Task.IsCompleted;

		/// <summary>
		/// Indicates whether cancellation has been requested.
		/// </summary>
		public bool IsCancellationRequested => this.Notifier.CancellationTokenSource?.IsCancellationRequested ?? false;

		/// <summary>
		/// Indicates whether the command is currently running.
		/// </summary>
		public bool IsRunning => this.Notifier.Task is { IsCompleted: false };
	}
}
