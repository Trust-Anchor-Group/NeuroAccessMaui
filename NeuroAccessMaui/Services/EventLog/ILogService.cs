using System.Runtime.CompilerServices;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.EventLog
{
	/// <summary>
	/// A log implementation for logging warnings, exceptions and events.
	/// </summary>
	[DefaultImplementation(typeof(LogService))]
	public interface ILogService : ILoadableService, IDisposable
	{
		/// <summary>
		/// Starts a debug log session. This log all events to a file
		/// </summary>
		Task StartDebugLogSessionAsync();

		/// <summary>
		/// Ends a debug log session. This Stops all events from being logged to a file.
		/// </summary>
		Task EndDebugLogSessionAsync();

		/// <summary>
		/// Adds an <see cref="IEventSink"/> to the log service. Any listeners will be called
		/// whenever any log event occurs.
		/// </summary>
		/// <param name="EventSink">The listener to add.</param>
		void AddListener(IEventSink EventSink);

		/// <summary>
		/// Removes an <see cref="IEventSink"/> to the log service.
		/// </summary>
		/// <param name="EventSink">The listener to remove.</param>
		void RemoveListener(IEventSink EventSink);

		/// <summary>
		/// Invoke this method to add a debug statement to the log.
		/// </summary>
		/// <param name="Message">Debug message.</param>
		/// <param name="Tags">Tags to log together with message.</param>
		public void LogDebug(string Message,
			params KeyValuePair<string, object?>[] Tags);

		/// <summary>
		/// Invoke this method to add a debug statement to the log.
		/// </summary>
		/// <param name="Message">Debug message.</param>
		/// <param name="LineNumber">The line of code of the caller</param>
		/// <param name="FilePath">THe file of the caller</param>
		public void LogDebug(string Message,
			[CallerFilePath] string FilePath = "",
			[CallerLineNumber] int LineNumber = 0);

		/// <summary>
		/// Invoke this method to add a warning statement to the log.
		/// </summary>
		/// <param name="Message">Warning message.</param>
		/// <param name="Tags">Tags to log together with message.</param>
		void LogWarning(string Message, params KeyValuePair<string, object?>[] Tags);

		/// <summary>
		/// Invoke this method to add an <see cref="Exception"/> entry to the log.
		/// </summary>
		/// <param name="ex">Exception object</param>
		void LogException(Exception ex);

		/// <summary>
		/// Invoke this method to add an <see cref="Exception"/> entry to the log.
		/// </summary>
		/// <param name="ex">The exception to log.</param>
		/// <param name="extraParameters">Any extra parameters that are added to the log.</param>
		void LogException(Exception ex, params KeyValuePair<string, object?>[] extraParameters);

		/// <summary>
		/// Invoke this method to add an alert statement to the log.
		/// </summary>
		/// <param name="Message">Alert message.</param>
		/// <param name="Tags">Tags to log together with message.</param>
		void LogAlert(string Message, params KeyValuePair<string, object?>[] Tags);

		/// <summary>
		/// Saves an exception dump to disc, completely offline. A last resort operation.
		/// </summary>
		/// <param name="Title">The title of the stack trace.</param>
		/// <param name="StackTrace">The actual stack trace.</param>
		void SaveExceptionDump(string Title, string StackTrace);

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
		IList<KeyValuePair<string, object?>> GetParameters(params KeyValuePair<string, object?>[] Tags);
	}
}