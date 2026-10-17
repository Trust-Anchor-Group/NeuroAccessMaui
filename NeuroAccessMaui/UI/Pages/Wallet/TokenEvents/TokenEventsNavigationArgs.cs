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
		/// Creates a new instance of the <see cref="TokenEventsNavigationArgs"/> class. The events page loads the events
		/// itself, page by page.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="IsMyToken">If the token belongs to the user, who can then add notes.</param>
		public TokenEventsNavigationArgs(string TokenId, bool IsMyToken)
		{
			this.TokenId = TokenId;
			this.IsMyToken = IsMyToken;
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
		/// The first events to show, newest first, when they are already loaded. The rest are loaded page by page.
		/// </summary>
		public TokenEvent[]? Events { get; }

		/// <summary>
		/// ID of the token's state machine, when it isn't the token ID. State changes reported for it refresh the events.
		/// </summary>
		public string? MachineId { get; init; }

		/// <summary>
		/// If the token belongs to the user, who can then add notes.
		/// </summary>
		public bool IsMyToken { get; }
	}
}
