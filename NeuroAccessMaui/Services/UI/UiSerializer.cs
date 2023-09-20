using NeuroAccessMaui.Services.UI.Tasks;
using System.Collections.Concurrent;
using System.Text;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI;

/// <inheritdoc/>
[Singleton]
public class UiSerializer : IUiSerializer
{
	private readonly ConcurrentQueue<UiTask> taskQueue;
	private bool isExecutingUiTasks;

	/// <summary>
	/// Creates a new instance of the <see cref="UiSerializer"/> class.
	/// </summary>
	public UiSerializer()
	{
		this.taskQueue = new ConcurrentQueue<UiTask>();

		this.isExecutingUiTasks = false;
		this.IsRunningInTheBackground = false;
	}

	/// <inheritdoc/>
	public void BeginInvokeOnMainThread(Action action)
	{
		Device.BeginInvokeOnMainThread(action);
	}

	/// <inheritdoc/>
	public Task InvokeOnMainThreadAsync(Action action)
	{
		return Device.InvokeOnMainThreadAsync(action);
	}

	/// <inheritdoc/>
	public bool IsRunningInTheBackground { get; set; }

	private void AddTask(UiTask Task)
	{
		this.taskQueue.Enqueue(Task);

		if (!this.isExecutingUiTasks)
		{
			this.isExecutingUiTasks = true;

			this.BeginInvokeOnMainThread(async () =>
			{
				await this.ProcessAllTasks();
			});
		}
	}

	private async Task ProcessAllTasks()
	{
		try
		{
			do
			{
				if (this.taskQueue.TryDequeue(out UiTask Task))
					await Task.Execute();
			}
			while (this.taskQueue.Count > 0);
		}
		finally
		{
			this.isExecutingUiTasks = false;
		}
	}

	#region DisplayAlert

	/// <inheritdoc/>
	public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
	{
		DisplayAlert Task = new(title, message, accept, cancel);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	/// <inheritdoc/>
	public Task DisplayAlert(string title, string message, string accept)
	{
		DisplayAlert Task = new(title, message, accept, null);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	/// <inheritdoc/>
	public Task DisplayAlert(string title, string message)
	{
		DisplayAlert Task = new(title, message, null, null);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	/// <inheritdoc/>
	public Task DisplayAlert(string title, string message, Exception exception)
	{
		exception = Log.UnnestException(exception);

		StringBuilder sb = new();

		if (exception is not null)
		{
			sb.AppendLine(exception.Message);

			while (!((exception = exception.InnerException) is null))
				sb.AppendLine(exception.Message);
		}
		else
			sb.AppendLine(LocalizationResourceManager.Current["ErrorTitle"]);

		return this.DisplayAlert(title ?? LocalizationResourceManager.Current["ErrorTitle"], sb.ToString(), LocalizationResourceManager.Current["Ok"]);
	}

	/// <inheritdoc/>
	public Task DisplayAlert(string title, Exception exception)
	{
		return this.DisplayAlert(title, null, exception);
	}

	/// <inheritdoc/>
	public Task DisplayAlert(Exception exception)
	{
		return this.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], null, exception);
	}

	#endregion

	#region DisplayPrompt

	/// <inheritdoc/>
	public Task<string> DisplayPrompt(string title, string message, string accept, string cancel)
	{
		DisplayPrompt Task = new(title, message, accept, cancel);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	/// <inheritdoc/>
	public Task<string> DisplayPrompt(string title, string message, string accept)
	{
		DisplayPrompt Task = new(title, message, accept, null);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	/// <inheritdoc/>
	public Task<string> DisplayPrompt(string title, string message)
	{
		DisplayPrompt Task = new(title, message, null, null);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	#endregion

}
