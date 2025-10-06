using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Reentrancy
{
	/// <summary>
	/// Simple asynchronous reentrancy guard that ensures a critical async section is not executed concurrently.
	/// </summary>
	public sealed class ReentrancyGuard
	{
		private int gate = 0;

		/// <summary>
		/// Attempts to execute the asynchronous action if no prior execution is in progress.
		/// Returns false if the action was skipped due to an in-progress execution.
		/// </summary>
		/// <param name="action">Asynchronous action.</param>
		public async Task<bool> RunIfNotBusy(Func<Task> action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));
			if (Interlocked.CompareExchange(ref this.gate, 1, 0) != 0)
			{
				return false; // Already running
			}
			try
			{
				await action().ConfigureAwait(false);
				return true;
			}
			finally
			{
				Interlocked.Exchange(ref this.gate, 0);
			}
		}

		/// <summary>
		/// Returns true if an execution is currently in progress.
		/// </summary>
		public bool IsBusy => this.gate != 0;
	}
}
