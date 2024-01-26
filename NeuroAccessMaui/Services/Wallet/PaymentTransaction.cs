using System.Globalization;

namespace NeuroAccessMaui.Services.Wallet
{
	/// <summary>
	/// Maintains the status of an ongoing payment transaction.
	/// </summary>
	/// <param name="TransactionId">Transaction ID</param>
	/// <param name="Currency">Currency</param>
	public class PaymentTransaction(string TransactionId, string Currency) : Transaction(TransactionId)
	{
		private readonly TaskCompletionSource<decimal?> result = new();
		private readonly string currency = Currency;

		/// <summary>
		/// Called when payment has failed.
		/// </summary>
		/// <param name="Message">Error message</param>
		public override void ErrorReported(string Message)
		{
			this.result.TrySetException(new Exception(Message));
		}

		/// <summary>
		/// Called when payment has completed.
		/// </summary>
		/// <param name="Amount">Amount</param>
		/// <param name="Currency">Currency</param>
		public void Completed(decimal Amount, string Currency)
		{
			if (string.Compare(this.currency, Currency, StringComparison.OrdinalIgnoreCase) == 0)
				this.result.TrySetResult(Amount);
			else
			{
				this.result.TrySetException(new Exception("Payment of " + Amount.ToString(CultureInfo.CurrentCulture.NumberFormat) +
					" " + Currency + " completed. Expected " + this.currency + "."));
			}
		}

		/// <summary>
		/// Waits for the completion of the payment.
		/// </summary>
		/// <returns>Amount transferred</returns>
		public Task<decimal?> Wait()
		{
			return this.result.Task;
		}
	}
}
