using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Policies
{
	/// <summary>
	/// Simple circuit breaker: trips after N consecutive failures and stays open for a cool-down.
	/// Allows a single probe during half-open; success closes, failure re-opens.
	/// </summary>
	sealed class CircuitBreakerPolicy : IAsyncPolicy
	{
		private enum State { Closed, Open, HalfOpen }

		private readonly int threshold;
		private readonly TimeSpan breakDuration;
		private readonly Action<string>? onStateChange;

		private int failures;
		private State state = State.Closed;
		private DateTimeOffset openedAt;
		private int halfOpenInProgress; // 0 or 1
		private readonly object sync = new();

		public CircuitBreakerPolicy(int failureThreshold, TimeSpan breakDuration, Action<string>? onStateChange = null)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(failureThreshold, 1);
			ArgumentOutOfRangeException.ThrowIfLessThan(breakDuration, TimeSpan.Zero);

			this.threshold = failureThreshold;
			this.breakDuration = breakDuration;
			this.onStateChange = onStateChange;
		}

		public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
		{
			// Gate based on state
			this.EnsureEntryAllowed();

			try
			{
				await action(ct).ConfigureAwait(false);
				this.OnSuccess();
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				// Forward cancellation without changing state
				throw;
			}
			catch
			{
				this.OnFailure();
				throw;
			}
		}

		public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
		{
			// Gate based on state
			this.EnsureEntryAllowed();

			try
			{
				T result = await action(ct).ConfigureAwait(false);
				this.OnSuccess();
				return result;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				// Forward cancellation without changing state
				throw;
			}
			catch
			{
				this.OnFailure();
				throw;
			}
		}

		private void EnsureEntryAllowed()
		{
			lock (this.sync)
			{
				switch (this.state)
				{
					case State.Closed:
						return;

					case State.Open:
						if (DateTimeOffset.UtcNow - this.openedAt >= this.breakDuration)
						{
							this.SetState(State.HalfOpen);
							// fall through to HalfOpen
							goto case State.HalfOpen;
						}
						throw new InvalidOperationException("Circuit is open");

					case State.HalfOpen:
						if (Interlocked.CompareExchange(ref this.halfOpenInProgress, 1, 0) == 0)
						{
							return; // allow single probe
						}
						throw new InvalidOperationException("Circuit is half-open; probe in progress");
				}
			}
		}

		private void OnSuccess()
		{
			lock (this.sync)
			{
				this.failures = 0;
				if (this.state != State.Closed)
					this.SetState(State.Closed);
				if (this.state != State.HalfOpen)
					Interlocked.Exchange(ref this.halfOpenInProgress, 0);
			}
		}

		private void OnFailure()
		{
			lock (this.sync)
			{
				if (this.state == State.HalfOpen)
				{
					this.Trip();
					Interlocked.Exchange(ref this.halfOpenInProgress, 0);
					return;
				}

				if (++this.failures >= this.threshold)
					this.Trip();
			}
		}

		private void Trip()
		{
			this.openedAt = DateTimeOffset.UtcNow;
			this.SetState(State.Open);
		}

		private void SetState(State newState)
		{
			if (this.state == newState)
				return;
			this.state = newState;
			this.onStateChange?.Invoke(newState.ToString());
		}
	}
}

