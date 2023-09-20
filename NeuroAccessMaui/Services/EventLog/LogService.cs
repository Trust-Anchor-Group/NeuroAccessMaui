using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.UI;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Persistence.Exceptions;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.EventLog;

[Singleton]
internal sealed class LogService : ILogService
{
	private const string startupCrashFileName = "CrashDump.txt";
	private string bareJid = string.Empty;
	private bool repairRequested = false;

	public void AddListener(IEventSink eventSink)
	{
		if (eventSink is XmppEventSink xmppEventSink)
		{
			this.bareJid = xmppEventSink.Client?.BareJID;
		}

		foreach (IEventSink Sink in Log.Sinks)
		{
			if (Sink == eventSink)
			{
				return;
			}
		}

		Log.Register(eventSink);
	}

	public void RemoveListener(IEventSink eventSink)
	{
		if (eventSink is not null)
		{
			Log.Unregister(eventSink);
		}
	}

	public void LogWarning(string Message, params KeyValuePair<string, object>[] Tags)
	{
		Log.Warning(Message, string.Empty, this.bareJid, this.GetParameters(Tags).ToArray());
	}

	public void LogException(Exception ex)
	{
		this.LogException(ex, null);
	}

	public void LogException(Exception ex, params KeyValuePair<string, object>[] extraParameters)
	{
		ex = Log.UnnestException(ex);

		Log.Critical(ex, string.Empty, this.bareJid, this.GetParameters(extraParameters).ToArray());

		if (ex is InconsistencyException && !this.repairRequested)
		{
			this.repairRequested = true;
			Task.Run(() => this.RestartForRepair());
		}
	}

	private async Task RestartForRepair()
	{
		IStorageService StorageService = App.Instantiate<IStorageService>();
		StorageService.FlagForRepair();

		IUiSerializer UiSerializer = App.Instantiate<IUiSerializer>();
		await UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["RepairRestart"], LocalizationResourceManager.Current["Ok"]);

		ICloseApplication CloseApplication = DependencyService.Get<ICloseApplication>();
		await CloseApplication.Close();
	}

	public void SaveExceptionDump(string title, string stackTrace)
	{
		stackTrace = Log.CleanStackTrace(stackTrace);

		string contents;
		string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

		if (File.Exists(fileName))
		{
			contents = File.ReadAllText(fileName);
		}
		else
		{
			contents = string.Empty;
		}

		File.WriteAllText(fileName, title + Environment.NewLine + stackTrace + Environment.NewLine + contents);
	}

	public string LoadExceptionDump()
	{
		string contents;
		string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

		if (File.Exists(fileName))
		{
			contents = File.ReadAllText(fileName);
		}
		else
		{
			contents = string.Empty;
		}

		return contents;
	}

	public void DeleteExceptionDump()
	{
		string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

		if (File.Exists(fileName))
		{
			File.Delete(fileName);
		}
	}

	///<inheritdoc/>
	public IList<KeyValuePair<string, object>> GetParameters(params KeyValuePair<string, object>[] Tags)
	{
		List<KeyValuePair<string, object>> Result = new()
		{
			new KeyValuePair<string, object>("Platform", Device.RuntimePlatform),
			new KeyValuePair<string, object>("RuntimeVersion", typeof(LogService).Assembly.ImageRuntimeVersion),
			new KeyValuePair<string, object>("AppVersion", AppInfo.VersionString),
			new KeyValuePair<string, object>("Manufacturer", DeviceInfo.Manufacturer),
			new KeyValuePair<string, object>("Device Model", DeviceInfo.Model),
			new KeyValuePair<string, object>("Device Name", DeviceInfo.Name),
			new KeyValuePair<string, object>("OS", DeviceInfo.VersionString),
			new KeyValuePair<string, object>("Platform", DeviceInfo.Platform.ToString()),
			new KeyValuePair<string, object>("Device Type", DeviceInfo.DeviceType.ToString()),
		};

		if (Tags is not null)
		{
			Result.AddRange(Tags);
		}

		return Result;
	}
}
