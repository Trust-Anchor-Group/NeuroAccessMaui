using Waher.Networking.XMPP.Push;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Contains information about a push notification token.
	/// </summary>
	public class TokenInformation
	{
		/// <summary>
		/// Token string
		/// </summary>
		public string? Token { get; set; }

		/// <summary>
		/// Service issuing the token
		/// </summary>
		public PushMessagingService Service { get; set; }

		/// <summary>
		/// Type of client
		/// </summary>
		public ClientType ClientType { get; set; }
	}
}
