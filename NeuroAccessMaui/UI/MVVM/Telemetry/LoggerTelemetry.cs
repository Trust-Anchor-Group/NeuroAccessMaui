using System;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.MVVM.Telemetry;

namespace NeuroAccessMaui.Telemetry
{
	/// <summary>
	/// Basic telemetry that writes ObservableTask events to the app log.
	/// </summary>
	public sealed class LoggerTelemetry : IObservableTaskTelemetry
	{
		public void OnEvent(ObservableTaskEvent e)
		{
			try
			{
				string Msg =
					$"[TaskTelemetry] Name={e.Name}, " +
					$"Status={e.Status}, " +
					$"Elapsed={e.Elapsed.TotalMilliseconds:F0}ms, " +
					$"IsRefresh={e.IsRefresh}";

				if (e.Exception is not null)
				{
					Msg += $", Exception={e.Exception.GetType().Name}: {e.Exception.Message}";
				}

				ServiceRef.LogService.LogInformational(Msg);
			}
			catch (Exception Ex)
			{
				// Never let telemetry exceptions break the app.
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
