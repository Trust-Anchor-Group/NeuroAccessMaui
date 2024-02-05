using Waher.Networking.XMPP.Push;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Delegate for token event handlers.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate Task TokenEventHandler(object? Sender, TokenEventArgs e);

	/// <summary>
	/// Event argumens for token-based events.
	/// </summary>
	public class TokenEventArgs : EventArgs
	{
		/// <summary>
		/// Event argumens for token-based events.
		/// </summary>
		/// <param name="Source">Source of notification</param>
		/// <param name="Token">Token</param>
		/// <param name="ClientType">Client Type</param>
		public TokenEventArgs(PushMessagingService Source, string Token, ClientType ClientType)
			: base()
		{
			this.Source = Source;
			this.Token = Token;
			this.ClientType = ClientType;
		}

		/// <summary>
		/// Source of notification
		/// </summary>
		public PushMessagingService Source { get; }

		/// <summary>
		/// Token
		/// </summary>
		public string Token { get; }

		/// <summary>
		/// Client Type
		/// </summary>
		public ClientType ClientType { get; }
	}
}
