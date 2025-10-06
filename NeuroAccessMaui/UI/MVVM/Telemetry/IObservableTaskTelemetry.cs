using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Telemetry
{

	public sealed record ObservableTaskEvent(
		string Name,
		ObservableTaskStatus Status,
		TimeSpan Elapsed,
		Exception? Exception = null,
		bool IsRefresh = false);
	public interface IObservableTaskTelemetry
	{
		void OnEvent(ObservableTaskEvent e);
	}
}
