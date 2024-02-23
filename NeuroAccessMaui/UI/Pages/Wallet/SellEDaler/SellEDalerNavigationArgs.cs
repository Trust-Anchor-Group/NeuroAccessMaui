using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Wallet.SellEDaler
{
	/// <summary>
	/// Holds navigation parameters specific to selling eDaler.
	/// </summary>
	public class SellEDalerNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SellEDalerNavigationArgs"/> class.
		/// </summary>
		public SellEDalerNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="SellEDalerNavigationArgs"/> class.
		/// </summary>
		/// <param name="Currency">Currency</param>
		/// <param name="Result">Result</param>
		public SellEDalerNavigationArgs(string Currency, TaskCompletionSource<decimal?> Result)
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
