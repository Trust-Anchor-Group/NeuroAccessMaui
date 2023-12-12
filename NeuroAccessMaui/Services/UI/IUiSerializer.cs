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
		/// <param name="Title">The title to display.</param>
		/// <param name="Message">The message to display.</param>
		/// <param name="Accept">The accept/ok button text.</param>
		/// <param name="Cancel">The cancel button text.</param>
		/// <returns>If Accept or Cancel was pressed</returns>
		Task<bool> DisplayAlert(string Title, string Message, string? Accept = null, string? Cancel = null);

		/// <summary>
		/// Displays an alert/message box to the user.
		/// </summary>
		/// <param name="Exception">The exception to display.</param>
		/// <param name="Title">The title to display.</param>
		Task DisplayException(Exception Exception, string? Title = null);

		#endregion

		#region DisplayPrompt

		/// <summary>
		/// Prompts the user for some input.
		/// </summary>
		/// <param name="Title">The title to display.</param>
		/// <param name="Message">The message to display.</param>
		/// <param name="Accept">The accept/ok button text.</param>
		/// <param name="Cancel">The cancel button text.</param>
		/// <returns>User input</returns>
		Task<string?> DisplayPrompt(string Title, string Message, string? Accept, string? Cancel);

		#endregion
	}
}
