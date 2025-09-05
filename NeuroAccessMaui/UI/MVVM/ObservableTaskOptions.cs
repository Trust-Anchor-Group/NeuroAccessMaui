using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.MVVM.Policies;
using NeuroAccessMaui.Services.Resilience.Dispatch;
using NeuroAccessMaui.UI.MVVM.Telemetry;

namespace NeuroAccessMaui.UI.MVVM
{
	public sealed class ObservableTaskOptions
	{
		public string Name { get; set; } = "Operation";
		public bool AutoStart { get; set; } = true;
		public bool UseTaskRun { get; set; } = false;
		public IObservableTaskTelemetry? Telemetry { get; set; }
		public List<IAsyncPolicy> Policies { get; } = new();
		public IDispatcherAdapter? Dispatcher { get; set; }
	}

}
