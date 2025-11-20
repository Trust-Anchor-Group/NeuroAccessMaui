using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Resilience
{
	/// <summary>
	/// Simple async-exclusive lock (lightweight). Each call to <see cref="LockAsync"/> yields a disposable releaser that must be disposed to release.
	/// </summary>
	public sealed class AsyncLock : IDisposable, IAsyncDisposable
	{
		private readonly SemaphoreSlim semaphore = new(1, 1);
		private bool disposed;

		/// <summary>
		/// Acquires the lock asynchronously, returning a disposable releaser that must be disposed exactly once.
		/// </summary>
		/// <param name="cancellationToken">Optional cancellation token.</param>
		public async ValueTask<Releaser> LockAsync(CancellationToken cancellationToken = default)
		{
			await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			return new Releaser(this);
		}

		/// <summary>
		/// Disposable releaser. Class (not struct) to avoid double-release via accidental copies.
		/// </summary>
		public sealed class Releaser : IDisposable, IAsyncDisposable
		{
			private AsyncLock? owner;
			internal Releaser(AsyncLock owner) { this.owner = owner; }
			public void Dispose()
			{
				AsyncLock? O = Interlocked.Exchange(ref this.owner, null);
				if (O is not null && !O.disposed)
				{
					O.semaphore.Release();
				}
			}
			public ValueTask DisposeAsync()
			{
				this.Dispose();
				return ValueTask.CompletedTask;
			}
		}

		public void Dispose()
		{
			if (this.disposed) return;
			this.disposed = true;
			this.semaphore.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			this.Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
