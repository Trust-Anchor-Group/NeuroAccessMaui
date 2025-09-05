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
	private readonly Func<Exception, bool>? shouldRetry;

	public RetryPolicy(int maxAttempts, Func<int, Exception, TimeSpan> delayProvider, Action<int, Exception, TimeSpan>? cb, Func<Exception, bool>? shouldRetry = null)
	{
		this.max = Math.Max(1, maxAttempts);
		this.delay = delayProvider;
		this.onRetry = cb;
		this.shouldRetry = shouldRetry;
	}

		public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
		{
			for (int Attempt = 1; ; Attempt++)
			{
				try { await action(ct); return; }
				catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
			catch (Exception Ex) when (Attempt < this.max && (this.shouldRetry?.Invoke(Ex) ?? true))
			{
				TimeSpan Wait = this.delay(Attempt, Ex);
				this.onRetry?.Invoke(Attempt, Ex, Wait);
				await Task.Delay(Wait, ct);
			}
		}
	}

	public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
	{
		for (int Attempt = 1; ; Attempt++)
		{
			try { return await action(ct).ConfigureAwait(false); }
			catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
			catch (Exception Ex) when (Attempt < this.max && (this.shouldRetry?.Invoke(Ex) ?? true))
			{
				TimeSpan Wait = this.delay(Attempt, Ex);
				this.onRetry?.Invoke(Attempt, Ex, Wait);
				await Task.Delay(Wait, ct).ConfigureAwait(false);
			}
		}
	}
}
}
