using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.UI.Tasks
{
	/// <summary>
	/// Displays an alert message.
	/// </summary>
	/// <param name="Title">Title string</param>
	/// <param name="Message">Message string</param>
	/// <param name="Accept">Accept string</param>
	/// <param name="Cancel">Cancel string</param>
	public class DisplayAlert(string Title, string Message, string? Accept, string? Cancel) : UiTask
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
		public TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

		/// <summary>
		/// Executes the task.
		/// </summary>
		public override async Task Execute()
		{
			bool Result;

			if (!string.IsNullOrWhiteSpace(this.Accept) && !string.IsNullOrWhiteSpace(this.Cancel))
			{
				Result = await (Application.Current?.MainPage?.DisplayAlert(this.Title, this.Message, this.Accept, this.Cancel) ??
					Task.FromResult(false));
			}
			else if (!string.IsNullOrWhiteSpace(this.Cancel))
			{
				await (Application.Current?.MainPage?.DisplayAlert(this.Title, this.Message, this.Cancel) ?? Task.CompletedTask);
				Result = true;
			}
			else if (!string.IsNullOrWhiteSpace(this.Accept))
			{
				await (Application.Current?.MainPage?.DisplayAlert(this.Title, this.Message, this.Accept) ?? Task.CompletedTask);
				Result = true;
			}
			else
			{
				await (Application.Current?.MainPage?.DisplayAlert(this.Title, this.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]) ?? Task.CompletedTask);

				Result = true;
			}

			this.CompletionSource.TrySetResult(Result);
		}
	}
}
