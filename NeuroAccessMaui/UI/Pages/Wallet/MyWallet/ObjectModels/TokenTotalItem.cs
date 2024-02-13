using NeuroFeatures;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels
{
	/// <summary>
	/// Encapsulates a <see cref="TokenTotal"/> object.
	/// </summary>
	/// <param name="Total">Token Total.</param>
	public class TokenTotalItem(TokenTotal Total) : IUniqueItem
	{
		private readonly TokenTotal total = Total;

		/// <inheritdoc/>
		public string UniqueName => this.total.Currency;

		/// <summary>
		/// Currency for sub-total
		/// </summary>
		public string Currency => this.total.Currency;

		/// <summary>
		/// Number of tokens in sub-total
		/// </summary>
		public int NrTokens => this.total.NrTokens;

		/// <summary>
		/// Sub-total
		/// </summary>
		public decimal Total => this.total.Total;
	}
}
