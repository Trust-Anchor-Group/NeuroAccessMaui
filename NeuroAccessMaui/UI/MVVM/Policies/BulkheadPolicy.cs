using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Policies
{
	/// <summary>
	/// Limits parallel executions, optionally bounding the queue. Rejects when saturated.
	/// </summary>
	sealed class BulkheadPolicy : IAsyncPolicy, IDisposable
	{
		private readonly SemaphoreSlim gate;
		private readonly int maxQueue;
		private readonly Action? onRejected;
		private int queued;
        private bool disposed;

		public BulkheadPolicy(int maxParallel, int maxQueue = 0, Action? onRejected = null)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(maxParallel, 1);
			ArgumentOutOfRangeException.ThrowIfLessThan(maxQueue, 0);

			this.gate = new SemaphoreSlim(maxParallel, maxParallel);
			this.maxQueue = maxQueue;
			this.onRejected = onRejected;
		}

		public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
		{
			ObjectDisposedException.ThrowIf(this.disposed, nameof(BulkheadPolicy));
			bool UsedQueueSlot = false;

			// Quick-path: If no slots and queue is disabled, reject immediately.
			if (this.gate.CurrentCount == 0 && this.maxQueue == 0)
			{
				this.onRejected?.Invoke();
				throw new InvalidOperationException("Bulkhead is full");
			}

			// Approximate queue bounding
			if (this.gate.CurrentCount == 0 && this.maxQueue > 0)
			{
				int q = Interlocked.Increment(ref this.queued);
				if (q > this.maxQueue)
				{
					Interlocked.Decrement(ref this.queued);
					this.onRejected?.Invoke();
					throw new InvalidOperationException("Bulkhead queue is full");
				}
				UsedQueueSlot = true;
			}

			await this.gate.WaitAsync(ct).ConfigureAwait(false);
			if (UsedQueueSlot)
				Interlocked.Decrement(ref this.queued);

			try
			{
				await action(ct).ConfigureAwait(false);
			}
			finally
			{
				this.gate.Release();
			}
		}

		public void Dispose()
		{
			if (this.disposed)
				return;
			this.disposed = true;
			this.gate.Dispose();
		}
	}
}
