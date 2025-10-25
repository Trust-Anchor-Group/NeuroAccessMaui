using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;
using NeuroAccessMaui.UI.MVVM.Telemetry;

namespace NeuroAccessMaui.UI.MVVM.Factory
{
	public sealed class ObservableTaskFactory : IObservableTaskFactory
	{
		private readonly IObservableTaskTelemetry? telemetry;

		public ObservableTaskFactory(IObservableTaskTelemetry? telemetry = null)
		{
			this.telemetry = telemetry;
		}

		public ObservableTask<TProgress> Create<TProgress>(
			string name,
			Func<TaskContext<TProgress>, Task> op,
			bool autoStart = true,
			bool useTaskRun = false,
			params IAsyncPolicy[] policies)
		{
			ObservableTaskBuilder<TProgress> Builder = new ObservableTaskBuilder<TProgress>()
				.Named(name)
				.AutoStart(autoStart)
				.UseTaskRun(useTaskRun)
				.Run(op);

			if(this.telemetry is not null)
				Builder.WithTelemetry(this.telemetry);

			foreach (IAsyncPolicy Policy in policies)
				Builder.WithPolicy(Policy);

			return Builder.Build();
		}
	}
}
