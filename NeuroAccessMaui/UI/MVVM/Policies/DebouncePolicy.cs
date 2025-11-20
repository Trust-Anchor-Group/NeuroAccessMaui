using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Policies
{
	/// <summary>
	/// Delays execution by a quiet period. If a new task run starts during this delay,
	/// the run is canceled via the outer CancellationToken (handled by ObservableTask).
	/// </summary>
	sealed class DebouncePolicy : IAsyncPolicy
	{
		private readonly TimeSpan delay;

		public DebouncePolicy(TimeSpan Delay)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(Delay, TimeSpan.Zero);
			this.delay = Delay;
		}

		public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
		{
			if (this.delay > TimeSpan.Zero)
				await Task.Delay(this.delay, ct);

			await action(ct);
		}

		public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
		{
			if (this.delay > TimeSpan.Zero)
				await Task.Delay(this.delay, ct);

			return await action(ct);
		}
	}
}

