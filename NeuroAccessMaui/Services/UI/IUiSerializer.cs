using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// A wafer-thin wrapper around the UI (main) thread.
	/// Provides methods for displaying alerts to the user in a thread-safe way. Queues them up if there's more than one.
	/// </summary>
	[DefaultImplementation(typeof(UiSerializer))]
	public interface IUiSerializer
	{
		#region DisplayAlert

		/// <summary>
		/// Displays an alert/message box to the user.
		/// </summary>
		/// <param name="title">The title to display.</param>
		/// <param name="message">The message to display.</param>
		/// <param name="accept">The accept/ok button text.</param>
		/// <param name="cancel">The cancel button text.</param>
		/// <returns>If Accept or Cancel was pressed</returns>
		Task<bool> DisplayAlert(string title, string message, string? accept = null, string? cancel = null);

		/// <summary>
		/// Displays an alert/message box to the user.
		/// </summary>
		/// <param name="exception">The exception to display.</param>
		/// <param name="title">The title to display.</param>
		Task DisplayException(Exception exception, string? title = null);

		#endregion

		#region DisplayPrompt

		/// <summary>
		/// Prompts the user for some input.
		/// </summary>
		/// <param name="title">The title to display.</param>
		/// <param name="message">The message to display.</param>
		/// <param name="accept">The accept/ok button text.</param>
		/// <param name="cancel">The cancel button text.</param>
		/// <returns>User input</returns>
		Task<string> DisplayPrompt(string title, string message, string? accept, string? cancel);

		#endregion
	}
}
