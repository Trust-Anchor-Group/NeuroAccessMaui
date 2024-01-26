using Waher.Persistence;

namespace NeuroAccessMaui.Services.Wallet
{
	/// <summary>
	/// Maintains the status of an ongoing retrieval of payment options.
	/// </summary>
	/// <param name="TransactionId">Transaction ID</param>
	public class OptionsTransaction(string TransactionId) : Transaction(TransactionId)
	{
		private readonly TaskCompletionSource<IDictionary<CaseInsensitiveString, object>[]> result = new();

		/// <summary>
		/// Called when procedure has failed.
		/// </summary>
		/// <param name="Message">Error message</param>
		public override void ErrorReported(string Message)
		{
			this.result.TrySetException(new Exception(Message));
		}

		/// <summary>
		/// Called when procedure has completed.
		/// </summary>
		/// <param name="Options">Available options.</param>
		public void Completed(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			this.result.TrySetResult(Options);
		}

		/// <summary>
		/// Waits for the completion of the procedure.
		/// </summary>
		/// <returns>Available options</returns>
		public Task<IDictionary<CaseInsensitiveString, object>[]> Wait()
		{
			return this.result.Task;
		}
	}
}
