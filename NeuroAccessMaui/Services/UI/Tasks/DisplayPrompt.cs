using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.UI.Tasks
{
	/// <summary>
	/// Prompts the user for input.
	/// </summary>
	/// <param name="Title">Title string</param>
	/// <param name="Message">Message string</param>
	/// <param name="Accept">Accept string</param>
	/// <param name="Cancel">Cancel string</param>
	public class DisplayPrompt(string Title, string Message, string? Accept, string? Cancel) : UiTask
	{
		/// <summary>
		/// Title string
		/// </summary>
		public string Title { get; } = Title;

		/// <summary>
		/// Message string
		/// </summary>
		public string Message { get; } = Message;

		/// <summary>
		/// Accept string
		/// </summary>
		public string? Accept { get; } = Accept;

		/// <summary>
		/// Cancel string
		/// </summary>
		public string? Cancel { get; } = Cancel;

		/// <summary>
		/// Completion source indicating when task has been completed.
		/// </summary>
		public TaskCompletionSource<string?> CompletionSource { get; } = new TaskCompletionSource<string?>();

		/// <summary>
		/// Executes the task.
		/// </summary>
		public override async Task Execute()
		{
			string? Result;

			Page? DisplayedPage = ServiceRef.UiService.PopupStack.LastOrDefault() ?? App.Current?.MainPage;

			if (DisplayedPage is null)
				Result = null;
			else if (!string.IsNullOrWhiteSpace(this.Accept) && !string.IsNullOrWhiteSpace(this.Cancel))
				Result = await DisplayedPage.DisplayPromptAsync(this.Title, this.Message, this.Accept, this.Cancel);
			else if (!string.IsNullOrWhiteSpace(this.Cancel))
				Result = await DisplayedPage.DisplayPromptAsync(this.Title, this.Message, this.Cancel);
			else if (!string.IsNullOrWhiteSpace(this.Accept))
				Result = await DisplayedPage.DisplayPromptAsync(this.Title, this.Message, this.Accept);
			else
			{
				Result = await DisplayedPage.DisplayPromptAsync(this.Title, this.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}

			this.CompletionSource.TrySetResult(Result);
		}
	}
}
