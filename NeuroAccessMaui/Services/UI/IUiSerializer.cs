using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI;

/// <summary>
/// A wafer-thin wrapper around the UI (main) thread.
/// Provides methods for displaying alerts to the user in a thread-safe way. Queues them up if there's more than one.
/// </summary>
[DefaultImplementation(typeof(UiSerializer))]
public interface IUiSerializer
{
	/// <summary>
	/// Does a begin-invoke on the main thread of the specified action.
	/// </summary>
	/// <param name="action">The action to execute asynchronously.</param>
	void BeginInvokeOnMainThread(Action action);

	/// <summary>
	/// Does a awaitable begin-invoke on the main thread of the specified action.
	/// </summary>
	/// <param name="action">The action to execute asynchronously.</param>
	Task InvokeOnMainThreadAsync(Action action);

	/// <summary>
	/// Determines whether the app is running in the background.
	/// </summary>
	bool IsRunningInTheBackground { get; set; }

	#region DisplayAlert

	/// <summary>
	/// Displays an alert/message box to the user.
	/// </summary>
	/// <param name="title">The title to display.</param>
	/// <param name="message">The message to display.</param>
	/// <param name="accept">The accept/ok button text.</param>
	/// <param name="cancel">The cancel button text.</param>
	/// <returns>If Accept or Cancel was pressed</returns>
	Task<bool> DisplayAlert(string title, string message, string accept, string cancel);

	/// <summary>
	/// Displays an alert/message box to the user.
	/// </summary>
	/// <param name="title">The title to display.</param>
	/// <param name="message">The message to display.</param>
	/// <param name="accept">The accept/ok button text.</param>
	Task DisplayAlert(string title, string message, string accept);

	/// <summary>
	/// Displays an alert/message box to the user.
	/// </summary>
	/// <param name="title">The title to display.</param>
	/// <param name="message">The message to display.</param>
	Task DisplayAlert(string title, string message);

	/// <summary>
	/// Displays an alert/message box to the user.
	/// </summary>
	/// <param name="title">The title to display.</param>
	/// <param name="message">The message to display.</param>
	/// <param name="exception">The exception to display.</param>
	Task DisplayAlert(string title, string message, Exception exception);

	/// <summary>
	/// Displays an alert/message box to the user.
	/// </summary>
	/// <param name="title">The title to display.</param>
	/// <param name="exception">The exception to display.</param>
	Task DisplayAlert(string title, Exception exception);

	/// <summary>
	/// Displays an alert/message box to the user.
	/// </summary>
	/// <param name="exception">The exception to display.</param>
	Task DisplayAlert(Exception exception);

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
	Task<string> DisplayPrompt(string title, string message, string accept, string cancel);

	/// <summary>
	/// Prompts the user for some input.
	/// </summary>
	/// <param name="title">The title to display.</param>
	/// <param name="message">The message to display.</param>
	/// <param name="accept">The accept/ok button text.</param>
	/// <returns>User input</returns>
	Task<string> DisplayPrompt(string title, string message, string accept);

	/// <summary>
	/// Prompts the user for some input.
	/// </summary>
	/// <param name="title">The title to display.</param>
	/// <param name="message">The message to display.</param>
	/// <returns>User input</returns>
	Task<string> DisplayPrompt(string title, string message);

	#endregion
}
