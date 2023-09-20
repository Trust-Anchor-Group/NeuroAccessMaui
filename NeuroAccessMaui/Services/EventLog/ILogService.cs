using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.EventLog;

/// <summary>
/// A log implementation for logging warnings, exceptions and events.
/// </summary>
[DefaultImplementation(typeof(LogService))]
public interface ILogService
{
	/// <summary>
	/// Adds an <see cref="IEventSink"/> to the log service. Any listeners will be called
	/// whenever any log event occurs.
	/// </summary>
	/// <param name="eventSink">The listener to add.</param>
	void AddListener(IEventSink eventSink);

	/// <summary>
	/// Removes an <see cref="IEventSink"/> to the log service.
	/// </summary>
	/// <param name="eventSink">The listener to remove.</param>
	void RemoveListener(IEventSink eventSink);

	/// <summary>
	/// Invoke this method to add a warning statement to the log.
	/// </summary>
	/// <param name="Message">Warning message.</param>
	/// <param name="Tags">Tags to log together with message.</param>
	void LogWarning(string Message, params KeyValuePair<string, object>[] Tags);

	/// <summary>
	/// Invoke this method to add an <see cref="Exception"/> entry to the log.
	/// </summary>
	/// <param name="e"></param>
	void LogException(Exception e);

	/// <summary>
	/// Invoke this method to add an <see cref="Exception"/> entry to the log.
	/// </summary>
	/// <param name="e">The exception to log.</param>
	/// <param name="extraParameters">Any extra parameters that are added to the log.</param>
	void LogException(Exception e, params KeyValuePair<string, object>[] extraParameters);

	/// <summary>
	/// Saves an exception dump to disc, completely offline. A last resort operation.
	/// </summary>
	/// <param name="title">The title of the stack trace.</param>
	/// <param name="stackTrace">The actual stack trace.</param>
	void SaveExceptionDump(string title, string stackTrace);

	/// <summary>
	/// Restores any exception dump that has previously been persisted with the <see cref="SaveExceptionDump"/> method.
	/// </summary>
	/// <returns>The exception dump, if it exists, or <c>null</c>.</returns>
	string LoadExceptionDump();

	/// <summary>
	/// Removes any exception dump from disc, if it exists.
	/// </summary>
	void DeleteExceptionDump();

	/// <summary>
	/// Gets a list of extra parameters that are useful when logging: Platform, RuntimeVersion, AppVersion.
	/// </summary>
	/// <param name="Tags">Extra tags</param>
	/// <returns>Parameters</returns>
	IList<KeyValuePair<string, object>> GetParameters(params KeyValuePair<string, object>[] Tags);
}
