using NeuroAccessMaui.Resources.Languages;
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
	private readonly ConcurrentQueue<UiTask> taskQueue = new();
	private bool isExecutingUiTasks = false;

	/// <summary>
	/// Creates a new instance of the <see cref="UiSerializer"/> class.
	/// </summary>
	public UiSerializer()
	{
	}

	private void AddTask(UiTask Task)
	{
		this.taskQueue.Enqueue(Task);

		if (!this.isExecutingUiTasks)
		{
			this.isExecutingUiTasks = true;

			MainThread.BeginInvokeOnMainThread(async () =>
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
				if (this.taskQueue.TryDequeue(out UiTask? Task))
				{
					await Task.Execute();
				}
			}
			while (!this.taskQueue.IsEmpty);
		}
		finally
		{
			this.isExecutingUiTasks = false;
		}
	}

	#region DisplayAlert

	/// <inheritdoc/>
	public Task<bool> DisplayAlert(string title, string message, string? accept = null, string? cancel = null)
	{
		DisplayAlert Task = new(title, message, accept, cancel);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	/// <inheritdoc/>
	public Task DisplayException(Exception exception, string? title = null)
	{
		exception = Log.UnnestException(exception);

		StringBuilder sb = new();

		if (exception is not null)
		{
			sb.AppendLine(exception.Message);

			while (exception.InnerException is not null)
			{
				exception = exception.InnerException;
				sb.AppendLine(exception.Message);
			}
		}
		else
		{
			sb.AppendLine(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)]);
		}

		return this.DisplayAlert(
			title ?? ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], sb.ToString(),
			ServiceRef.Localizer[nameof(AppResources.Ok)]);
	}

	#endregion

	#region DisplayPrompt

	/// <inheritdoc/>
	public Task<string> DisplayPrompt(string title, string message, string? accept = null, string? cancel = null)
	{
		DisplayPrompt Task = new(title, message, accept, cancel);
		this.AddTask(Task);
		return Task.CompletionSource.Task;
	}

	#endregion

}
