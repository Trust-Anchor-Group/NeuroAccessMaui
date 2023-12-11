using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.UI.Tasks
{
	/// <summary>
	/// Displays an alert message.
	/// </summary>
	public class DisplayAlert : UiTask
	{
		/// <summary>
		/// Displays an alert message.
		/// </summary>
		/// <param name="Title">Title string</param>
		/// <param name="Message">Message string</param>
		/// <param name="Accept">Accept string</param>
		/// <param name="Cancel">Cancel string</param>
		public DisplayAlert(string Title, string Message, string? Accept, string? Cancel)
		{
			this.Title = Title;
			this.Message = Message;
			this.Accept = Accept;
			this.Cancel = Cancel;

			this.CompletionSource = new TaskCompletionSource<bool>();
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
		public TaskCompletionSource<bool> CompletionSource { get; }

		/// <summary>
		/// Executes the task.
		/// </summary>
		public override async Task Execute()
		{
			bool Result;

			if (!string.IsNullOrWhiteSpace(this.Accept) && !string.IsNullOrWhiteSpace(this.Cancel))
			{
				Result = await Application.Current.MainPage.DisplayAlert(this.Title, this.Message, this.Accept, this.Cancel);
			}
			else if (!string.IsNullOrWhiteSpace(this.Cancel))
			{
				await Application.Current.MainPage.DisplayAlert(this.Title, this.Message, this.Cancel);
				Result = true;
			}
			else if (!string.IsNullOrWhiteSpace(this.Accept))
			{
				await Application.Current.MainPage.DisplayAlert(this.Title, this.Message, this.Accept);
				Result = true;
			}
			else
			{
				await Application.Current.MainPage.DisplayAlert(this.Title, this.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);

				Result = true;
			}

			this.CompletionSource.TrySetResult(Result);
		}
	}
}
