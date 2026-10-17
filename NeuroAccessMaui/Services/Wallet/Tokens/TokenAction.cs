namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Something a user can do with a token through a smart contract.
	/// </summary>
	public enum TokenAction
	{
		/// <summary>
		/// Create new tokens.
		/// </summary>
		Create,

		/// <summary>
		/// Offer a token you own for sale.
		/// </summary>
		Sell,

		/// <summary>
		/// Offer to buy a token someone else owns.
		/// </summary>
		Buy,

		/// <summary>
		/// Consign a token you own to an auctioneer, who sells it on the marketplace.
		/// </summary>
		Consign,

		/// <summary>
		/// Give a token you own to someone else.
		/// </summary>
		Give,

		/// <summary>
		/// Destroy a token you own.
		/// </summary>
		Destroy
	}
}
