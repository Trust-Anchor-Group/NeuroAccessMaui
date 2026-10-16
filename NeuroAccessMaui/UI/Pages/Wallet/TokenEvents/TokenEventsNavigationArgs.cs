using NeuroAccessMaui.Services.UI;
using NeuroFeatures.Events;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// Holds navigation parameters containing the events of a token.
	/// </summary>
	public class TokenEventsNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenEventsNavigationArgs"/> class.
		/// </summary>
		public TokenEventsNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="TokenEventsNavigationArgs"/> class.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="Events">Token events</param>
		public TokenEventsNavigationArgs(string TokenId, TokenEvent[] Events)
			: this(TokenId, Events, false)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="TokenEventsNavigationArgs"/> class.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="Events">Token events</param>
		/// <param name="IsMyToken">If the token belongs to the user, who can then add notes.</param>
		public TokenEventsNavigationArgs(string TokenId, TokenEvent[] Events, bool IsMyToken)
		{
			this.TokenId = TokenId;
			this.Events = Events;
			this.IsMyToken = IsMyToken;
		}

		/// <summary>
		/// Token ID
		/// </summary>
		public string? TokenId { get; }

		/// <summary>
		/// Token events
		/// </summary>
		public TokenEvent[]? Events { get; }

		/// <summary>
		/// If the token belongs to the user, who can then add notes.
		/// </summary>
		public bool IsMyToken { get; }
	}
}
