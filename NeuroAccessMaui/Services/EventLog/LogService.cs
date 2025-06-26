using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using NeuroAccessMaui.Resources.Languages;
using Waher.Events;
using Waher.Events.Files;
using Waher.Events.Filter;
using Waher.Events.XMPP;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements;
using Waher.Persistence.Exceptions;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.EventLog
{
	[Singleton]
	internal sealed class LogService : LoadableService, ILogService
	{
		private const string startupCrashFileName = "CrashDump.txt";
		private const string debugLogFileName = "Log.txt";
		private string bareJid = string.Empty;
		private bool repairRequested = false;

		private TextWriterEventSink? debugSink;
		private TextWriter? debugTextWriter;
		private FileStream? debugFileStream;

		/// <summary>
		/// Adds an <see cref="IEventSink"/> to the log service. Any listeners will be called
		/// whenever any log event occurs.
		/// </summary>
		/// <param name="EventSink">The listener to add.</param>
		public void AddListener(IEventSink EventSink)
		{
			if (EventSink is EventFilter EventFilter && EventFilter.SecondarySink is XmppEventSink XmppEventSink)
				this.bareJid = XmppEventSink.Client?.BareJID ?? string.Empty;

			foreach (IEventSink Sink in Log.Sinks)
			{
				if (Sink == EventSink)
					return;
			}
			Log.Register(EventSink);
		}

		/// <summary>
		/// Removes an <see cref="IEventSink"/> to the log service.
		/// </summary>
		/// <param name="EventSink">The listener to remove.</param>
		public void RemoveListener(IEventSink EventSink)
		{
			if (EventSink is not null)
				Log.Unregister(EventSink);
		}

		/// <summary>
		/// Invoke this method to add a debug statement to the log.
		/// </summary>
		/// <param name="Message">Debug message.</param>
		/// <param name="Tags">Tags to log together with message.</param>
		public void LogDebug(string Message,
			params KeyValuePair<string, object?>[] Tags)
		{
			Log.Debug(Message,
				string.Empty,
				this.bareJid,
				Tags);
		}

		/// <summary>
		/// Invoke this method to add a debug statement to the log.
		/// </summary>
		/// <param name="Message">Debug message.</param>
		/// <param name="LineNumber">The line of code of the caller</param>
		/// <param name="FilePath">THe file of the caller</param>
		public void LogDebug(string Message,
			[CallerFilePath] string FilePath = "",
			[CallerLineNumber] int LineNumber = 0)
		{
			Log.Debug($"{Message} \n (File: {FilePath}, Line: {LineNumber})",
				string.Empty,
				this.bareJid,
				[]);
		}


		/// <summary>
		/// Invoke this method to add an informational statement to the log.
		/// </summary>
		/// <param name="Message">Info message</param>
		/// <param name="Tags">Tags to log together with message</param>
		public void LogInformational(string Message, params KeyValuePair<string, object?>[] Tags)
		{
			Log.Informational(Message, string.Empty, this.bareJid, [.. this.GetParameters(Tags)]);
		}

		/// <summary>
		/// Invoke this method to add a warning statement to the log.
		/// </summary>
		/// <param name="Message">Warning message.</param>
		/// <param name="Tags">Tags to log together with message.</param>
		public void LogWarning(string Message, params KeyValuePair<string, object?>[] Tags)
		{
			Log.Warning(Message, string.Empty, this.bareJid, [.. this.GetParameters(Tags)]);
		}

		/// <summary>
		/// Invoke this method to add an <see cref="Exception"/> entry to the log.
		/// </summary>
		/// <param name="ex">Exception object</param>
		public void LogException(Exception ex)
		{
			this.LogException(ex, []);
		}

		/// <summary>
		/// Invoke this method to add an <see cref="Exception"/> entry to the log.
		/// </summary>
		/// <param name="ex">The exception to log.</param>
		/// <param name="extraParameters">Any extra parameters that are added to the log.</param>
		public void LogException(Exception ex, params KeyValuePair<string, object?>[] extraParameters)
		{
			ex = Log.UnnestException(ex);

			Debug.WriteLine(ex.ToString());
			Log.Exception(ex, string.Empty, this.bareJid, [.. this.GetParameters(extraParameters)]);

			if (ex is InconsistencyException && !this.repairRequested)
			{
				this.repairRequested = true;
				Task.Run(RestartForRepair);
			}
		}

		/// <summary>
		/// Invoke this method to add an alert statement to the log.
		/// </summary>
		/// <param name="Message">Alert message.</param>
		/// <param name="Tags">Tags to log together with message.</param>
		public void LogAlert(string Message, params KeyValuePair<string, object?>[] Tags)
		{
			Log.Alert(Message, string.Empty, this.bareJid, [.. this.GetParameters(Tags)]);
		}

		private static async Task RestartForRepair()
		{
			ServiceRef.StorageService.FlagForRepair();

			await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				ServiceRef.Localizer[nameof(AppResources.RepairRestart)],
				ServiceRef.Localizer[nameof(AppResources.Ok)]);

			try
			{
				await ServiceRef.PlatformSpecific.CloseApplication();
			}
			catch (Exception)
			{
				Environment.Exit(0);
			}
		}

		/// <summary>
		/// Saves an exception dump to disc, completely offline. A last resort operation.
		/// </summary>
		/// <param name="Title">The title of the stack trace.</param>
		/// <param name="StackTrace">The actual stack trace.</param>
		public void SaveExceptionDump(string Title, string StackTrace)
		{
			StackTrace = Log.CleanStackTrace(StackTrace);

			string Contents;
			string FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

			if (File.Exists(FileName))
				Contents = File.ReadAllText(FileName);
			else
				Contents = string.Empty;

			File.WriteAllText(FileName, Title + Environment.NewLine + StackTrace + Environment.NewLine + Contents);
		}

		/// <summary>
		/// Restores any exception dump that has previously been persisted with the <see cref="SaveExceptionDump"/> method.
		/// </summary>
		/// <returns>The exception dump, if it exists, or <c>null</c>.</returns>
		public string LoadExceptionDump()
		{
			string contents;
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

			if (File.Exists(fileName))
				contents = File.ReadAllText(fileName);
			else
				contents = string.Empty;

			return contents;
		}

		/// <summary>
		/// Removes any exception dump from disc, if it exists.
		/// </summary>
		public void DeleteExceptionDump()
		{
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

			if (File.Exists(fileName))
				File.Delete(fileName);
		}

		/// <summary>
		/// Gets a list of extra parameters that are useful when logging: Platform, RuntimeVersion, AppVersion.
		/// </summary>
		/// <param name="Tags">Extra tags</param>
		/// <returns>Parameters</returns>
		public IList<KeyValuePair<string, object?>> GetParameters(params KeyValuePair<string, object?>[] Tags)
		{
			List<KeyValuePair<string, object?>> Result =
			[
				new KeyValuePair<string, object?>("Platform", DeviceInfo.Platform),
				new KeyValuePair<string, object?>("RuntimeVersion", typeof(LogService).Assembly.ImageRuntimeVersion),
				new KeyValuePair<string, object?>("AppVersion", AppInfo.VersionString),
				new KeyValuePair<string, object?>("Manufacturer", DeviceInfo.Manufacturer),
				new KeyValuePair<string, object?>("Device Model", DeviceInfo.Model),
				new KeyValuePair<string, object?>("Device Name", DeviceInfo.Name),
				new KeyValuePair<string, object?>("OS", DeviceInfo.VersionString),
				new KeyValuePair<string, object?>("Platform", DeviceInfo.Platform.ToString()),
				new KeyValuePair<string, object?>("Device Type", DeviceInfo.DeviceType.ToString()),
			];

			if (Tags is not null)
				Result.AddRange(Tags);

			return Result;
		}

		public override Task Load(bool isResuming, CancellationToken cancellationToken)
		{
#if DEBUG
			this.AddListener(new DebugEventSink());
#endif
			return Task.CompletedTask;
		}
		public async Task StartDebugLogSessionAsync()
		{
			string FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), debugLogFileName);

			// If an old session is still open, end it first
			if (this.debugSink != null)
				await this.EndDebugLogSessionAsync();

			if (File.Exists(FileName))
				File.Delete(FileName);

			// Create and keep references
			this.debugFileStream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			this.debugTextWriter = new StreamWriter(this.debugFileStream, Encoding.UTF8)
			{
				AutoFlush = true
			};
			this.debugSink = new TextWriterEventSink(debugLogFileName, this.debugTextWriter);

			this.AddListener(this.debugSink);
		}

		public async Task EndDebugLogSessionAsync()
		{
			if (this.debugSink is null)
				return;

			// Unregister the sink so no more events are written
			this.RemoveListener(this.debugSink);

			// Dispose the sink (it doesn't own the writer, so no-op, but good practice)
			await this.debugSink.DisposeAsync();
			this.debugSink = null;

			// Dispose the writer and the file stream
			this.debugTextWriter?.Dispose();
			this.debugTextWriter = null;

			this.debugFileStream?.Dispose();
			this.debugFileStream = null;
		}

        /// <summary>
        /// Disposes any active debug log session and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            // If a debug session is still open, clean it up synchronously
            if (this.debugSink is not null || this.debugTextWriter is not null || this.debugFileStream is not null)
            {
                // Block on the async cleanup
                this.EndDebugLogSessionAsync().GetAwaiter().GetResult();
            }

            GC.SuppressFinalize(this);
        }
	}
}
