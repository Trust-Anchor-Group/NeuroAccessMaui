using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Holds navigation parameters specific to a token.
	/// </summary>
	public class TokenDetailsNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenDetailsNavigationArgs"/> class.
		/// </summary>
		public TokenDetailsNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="TokenDetailsNavigationArgs"/> class.
		/// </summary>
		/// <param name="Token">Information about a token.</param>
		public TokenDetailsNavigationArgs(TokenItem Token)
			: this(Token, TokenOrigin.Wallet, false)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="TokenDetailsNavigationArgs"/> class.
		/// </summary>
		/// <param name="Token">Information about a token.</param>
		/// <param name="Origin">Where the token comes from.</param>
		/// <param name="ChangedSinceShared">If the token has changed since a copy of it was shared.</param>
		public TokenDetailsNavigationArgs(TokenItem Token, TokenOrigin Origin, bool ChangedSinceShared)
		{
			this.Token = Token;
			this.Origin = Origin;
			this.ChangedSinceShared = ChangedSinceShared;
		}

		/// <summary>
		/// Token
		/// </summary>
		public TokenItem? Token { get; }

		/// <summary>
		/// Where the token comes from.
		/// </summary>
		public TokenOrigin Origin { get; } = TokenOrigin.Wallet;

		/// <summary>
		/// If the token has changed since a copy of it was shared.
		/// </summary>
		public bool ChangedSinceShared { get; }
	}
}
