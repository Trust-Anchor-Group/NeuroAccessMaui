using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.UI.Tasks
{
	/// <summary>
	/// Prompts the user for input.
	/// </summary>
	public class DisplayPrompt : UiTask
	{
		/// <summary>
		/// Prompts the user for input.
		/// </summary>
		/// <param name="Title">Title string</param>
		/// <param name="Message">Message string</param>
		/// <param name="Accept">Accept string</param>
		/// <param name="Cancel">Cancel string</param>
		public DisplayPrompt(string Title, string Message, string? Accept, string? Cancel)
		{
			this.Title = Title;
			this.Message = Message;
			this.Accept = Accept;
			this.Cancel = Cancel;

			this.CompletionSource = new TaskCompletionSource<string>();
		}

		/// <summary>
		/// Title string
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Message string
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Accept string
		/// </summary>
		public string? Accept { get; }

		/// <summary>
		/// Cancel string
		/// </summary>
		public string? Cancel { get; }

		/// <summary>
		/// Completion source indicating when task has been completed.
		/// </summary>
		public TaskCompletionSource<string> CompletionSource { get; }

		/// <summary>
		/// Executes the task.
		/// </summary>
		public override async Task Execute()
		{
			string Result;

			if (!string.IsNullOrWhiteSpace(this.Accept) && !string.IsNullOrWhiteSpace(this.Cancel))
			{
				Result = await Application.Current.MainPage.DisplayPromptAsync(this.Title, this.Message, this.Accept, this.Cancel);
			}
			else if (!string.IsNullOrWhiteSpace(this.Cancel))
			{
				Result = await Application.Current.MainPage.DisplayPromptAsync(this.Title, this.Message, this.Cancel);
			}
			else if (!string.IsNullOrWhiteSpace(this.Accept))
			{
				Result = await Application.Current.MainPage.DisplayPromptAsync(this.Title, this.Message, this.Accept);
			}
			else
			{
				Result = await Application.Current.MainPage.DisplayPromptAsync(this.Title, this.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}

			this.CompletionSource.TrySetResult(Result);
		}
	}
}
