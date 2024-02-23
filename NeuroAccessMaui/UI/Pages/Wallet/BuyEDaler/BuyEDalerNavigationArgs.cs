using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler
{
	/// <summary>
	/// Holds navigation parameters specific to buying eDaler.
	/// </summary>
	public class BuyEDalerNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="BuyEDalerNavigationArgs"/> class.
		/// </summary>
		public BuyEDalerNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="BuyEDalerNavigationArgs"/> class.
		/// </summary>
		/// <param name="Currency">Currency</param>
		/// <param name="Result">Result</param>
		public BuyEDalerNavigationArgs(string Currency, TaskCompletionSource<decimal?> Result)
		{
			this.Currency = Currency;
			this.Result = Result;
		}

		/// <summary>
		/// Currency
		/// </summary>
		public string? Currency { get; }

		/// <summary>
		/// Amount, or null if user cancels operation.
		/// </summary>
		public TaskCompletionSource<decimal?>? Result { get; }
	}
}
