using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.MVVM.Policies;
using NeuroAccessMaui.UI.MVVM.Telemetry;

namespace NeuroAccessMaui.UI.MVVM.Building
{
	public class ObservableTaskBuilder<TProgress>
	{
		private readonly ObservableTaskOptions options = new();
		private Func<TaskContext<TProgress>, Task>? factory;

		public ObservableTaskBuilder<TProgress> Named(string name) { this.options.Name = name; return this; }
		public ObservableTaskBuilder<TProgress> AutoStart(bool value = true) { this.options.AutoStart = value; return this; }
		public ObservableTaskBuilder<TProgress> UseTaskRun(bool value = true) { this.options.UseTaskRun = value; return this; }
		public ObservableTaskBuilder<TProgress> WithPolicy(IAsyncPolicy policy) { this.options.Policies.Add(policy); return this; }
		public ObservableTaskBuilder<TProgress> WithTelemetry(IObservableTaskTelemetry telemetry) { this.options.Telemetry = telemetry; return this; }
		public ObservableTaskBuilder<TProgress> Run(Func<TaskContext<TProgress>, Task> op) { this.factory = op; return this; }

		public ObservableTask<TProgress> Build(params IRelayCommand[] notify)
		{
			if (this.factory is null)
				throw new InvalidOperationException("Factory not set.");

			Func<TaskContext<TProgress>, Task> Wrapped = async ctx =>
			{
				System.Diagnostics.Stopwatch Sw = System.Diagnostics.Stopwatch.StartNew();
				try
				{
					// Build policy pipeline outer -> inner
					Func<CancellationToken, Task> Core =
						ct => this.factory(new TaskContext<TProgress>(ctx.IsRefreshing, ct, ctx.Progress));

					Func<CancellationToken, Task> Pipeline = Core;

					foreach (IAsyncPolicy? Policy in Enumerable.Reverse(this.options.Policies))
					{
						Func<CancellationToken, Task> Next = Pipeline;
						Pipeline = ct => Policy.ExecuteAsync(Next, ct);
					}

					await Pipeline(ctx.CancellationToken);
					this.options.Telemetry?.OnEvent(new(this.options.Name, ObservableTaskStatus.Succeeded, Sw.Elapsed, null, ctx.IsRefreshing));
				}
				catch (OperationCanceledException oce) when (ctx.CancellationToken.IsCancellationRequested)
				{
					this.options.Telemetry?.OnEvent(new(this.options.Name, ObservableTaskStatus.Canceled, Sw.Elapsed, oce, ctx.IsRefreshing));
					throw;
				}
				catch (Exception ex)
				{
					this.options.Telemetry?.OnEvent(new(this.options.Name, ObservableTaskStatus.Failed, Sw.Elapsed, ex, ctx.IsRefreshing));
					throw;
				}
			};

			// Create the task here so the builder doesn't own a disposable field (fixes CA1001).
			ObservableTask<TProgress> Task = new ObservableTask<TProgress>
			{
				UseTaskRun = this.options.UseTaskRun
			};

			Task.Configure(Wrapped, notify);

			// Attach disposable policies for cleanup when the task is disposed
			foreach (IAsyncPolicy Policy in this.options.Policies)
			{
				if (Policy is IDisposable d)
					Task.AttachDisposable(d);
			}

			if (this.options.AutoStart)
				Task.Run();

			return Task;
		}
	}

	public class ObservableTaskBuilder : ObservableTaskBuilder<int>
	{
	}
}
