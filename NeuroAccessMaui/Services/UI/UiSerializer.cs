using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.UI.Tasks;
using System.Collections.Concurrent;
using System.Text;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI
{
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
						await Task.Execute();
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
		public Task<bool> DisplayAlert(string Title, string Message, string? Accept = null, string? Cancel = null)
		{
			DisplayAlert Task = new(Title, Message, Accept, Cancel);
			this.AddTask(Task);
			return Task.CompletionSource.Task;
		}

		/// <inheritdoc/>
		public Task DisplayException(Exception Exception, string? Title = null)
		{
			Exception = Log.UnnestException(Exception);

			StringBuilder sb = new();

			if (Exception is not null)
			{
				sb.AppendLine(Exception.Message);

				while (Exception.InnerException is not null)
				{
					Exception = Exception.InnerException;
					sb.AppendLine(Exception.Message);
				}
			}
			else
				sb.AppendLine(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)]);

			return this.DisplayAlert(
				Title ?? ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], sb.ToString(),
				ServiceRef.Localizer[nameof(AppResources.Ok)]);
		}

		#endregion

		#region DisplayPrompt

		/// <inheritdoc/>
		public Task<string?> DisplayPrompt(string Title, string Message, string? Accept = null, string? Cancel = null)
		{
			DisplayPrompt Task = new(Title, Message, Accept, Cancel);
			this.AddTask(Task);
			return Task.CompletionSource.Task;
		}

		#endregion

	}
}
