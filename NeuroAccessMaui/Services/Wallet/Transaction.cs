namespace NeuroAccessMaui.Services.Wallet
{
	/// <summary>
	/// Abstract base class for transactions.
	/// </summary>
	/// <param name="TransactionId">Transaction ID</param>
	public abstract class Transaction(string TransactionId)
	{
		/// <summary>
		/// Transaction ID
		/// </summary>
		public string TransactionId { get; } = TransactionId;

		/// <summary>
		/// Method called when a URL needs to be opened by the client.
		/// </summary>
		public static async Task OpenUrl(string Url)
		{
			await App.OpenUrlAsync(Url);
		}

		/// <summary>
		/// Called when payment has failed.
		/// </summary>
		/// <param name="Message">Error message</param>
		public abstract void ErrorReported(string Message);
	}
}
