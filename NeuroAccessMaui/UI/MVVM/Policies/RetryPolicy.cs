using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Policies
{
	sealed class RetryPolicy : IAsyncPolicy
	{
		private readonly int max;
		private readonly Func<int, Exception, TimeSpan> delay;
		private readonly Action<int, Exception, TimeSpan>? onRetry;

		public RetryPolicy(int maxAttempts, Func<int, Exception, TimeSpan> delayProvider, Action<int, Exception, TimeSpan>? cb)
		{
			this.max = Math.Max(1, maxAttempts);
			this.delay = delayProvider;
			this.onRetry = cb;
		}

		public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
		{
			for (int Attempt = 1; ; Attempt++)
			{
				try { await action(ct); return; }
				catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
				catch (Exception Ex) when (Attempt < this.max)
				{
					TimeSpan Wait = this.delay(Attempt, Ex);
					this.onRetry?.Invoke(Attempt, Ex, Wait);
					await Task.Delay(Wait, ct);
				}
			}
		}
	}
}
