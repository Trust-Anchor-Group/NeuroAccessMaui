namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Where a token shown in token details comes from, which decides how far it can be trusted.
	/// </summary>
	public enum TokenOrigin
	{
		/// <summary>
		/// From the user's own wallet listing.
		/// </summary>
		Wallet,

		/// <summary>
		/// Fetched from the token's Neuron just now.
		/// </summary>
		Live,

		/// <summary>
		/// A copy someone shared, which couldn't be checked with the token's Neuron.
		/// </summary>
		UnverifiedCopy
	}
}
