using NeuroAccessMaui.Services.UI;
using NeuroFeatures;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Holds navigation parameters for the canonical token workspace.
	/// </summary>
	public class TokenDetailsNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates empty token navigation arguments for framework compatibility.
		/// </summary>
		public TokenDetailsNavigationArgs()
		{
		}

		/// <summary>
		/// Creates navigation arguments from a durable token identifier and optional immediate data.
		/// </summary>
		/// <param name="TokenId">The durable identifier of the token to display.</param>
		/// <param name="InitialToken">Optional already-loaded token data for immediate presentation.</param>
		public TokenDetailsNavigationArgs(string TokenId, Token? InitialToken = null)
		{
			this.TokenId = TokenId;
			this.InitialToken = InitialToken;
		}

		/// <summary>
		/// Gets the durable identifier of the token to display.
		/// </summary>
		public string? TokenId { get; }

		/// <summary>
		/// Gets optional already-loaded token data for immediate presentation.
		/// </summary>
		public Token? InitialToken { get; }
	}
}
