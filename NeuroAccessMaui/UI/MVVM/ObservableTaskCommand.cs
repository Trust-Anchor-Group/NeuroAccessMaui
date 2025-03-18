using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.MVVM
{
	[Flags]
	public enum ObservableTaskCommandOptions
	{
		None = 0,
		AllowConcurrentRestart = 1 << 0,
	}

	public class ObservableTaskCommand<TResult, TProgress> : IAsyncRelayCommand
	{
		private readonly Func<TaskContext<TProgress>, Task<TResult>> taskFactory;
		private readonly Func<bool>? canExecute;
		private readonly ObservableTaskCommandOptions options;

		public ObservableTask<TResult, TProgress> Notifier { get; }

		// Constructor without canExecute (defaults to always executable and no options)
		public ObservableTaskCommand(
			Func<TaskContext<TProgress>, Task<TResult>> taskFactory)
			 : this(taskFactory, null, ObservableTaskCommandOptions.None)
		{
		}

		// Constructor with optional canExecute and options
		public ObservableTaskCommand(
			Func<TaskContext<TProgress>, Task<TResult>> taskFactory,
			 Func<bool>? canExecute,
			 ObservableTaskCommandOptions options = ObservableTaskCommandOptions.None)
		{

			this.taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
			this.canExecute = canExecute;
			this.options = options;
			this.Notifier = new ObservableTask<TResult, TProgress>();
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
			Task<TResult>? Task = this.Notifier.CurrentTask;
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
			if ((this.options & ObservableTaskCommandOptions.AllowConcurrentRestart) == 0)
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
		public Task? ExecutionTask => this.Notifier.CurrentTask;

		/// <summary>
		/// Indicates whether the command can be canceled.
		/// </summary>
		public bool CanBeCanceled =>
			 this.Notifier.CancellationTokenSource is not null &&
			 this.Notifier.CurrentTask is not null &&
			 !this.Notifier.CurrentTask.IsCompleted;

		/// <summary>
		/// Indicates whether cancellation has been requested.
		/// </summary>
		public bool IsCancellationRequested => this.Notifier.CancellationTokenSource?.IsCancellationRequested ?? false;

		/// <summary>
		/// Indicates whether the command is currently running.
		/// </summary>
		public bool IsRunning => this.Notifier.CurrentTask is { IsCompleted: false };
	}
}
