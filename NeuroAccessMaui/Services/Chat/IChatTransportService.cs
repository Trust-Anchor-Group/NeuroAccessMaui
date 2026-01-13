using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Events;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Abstraction over the underlying XMPP transport for chat messaging.
	/// </summary>
	public interface IChatTransportService
	{
		/// <summary>
		/// Raised when a new message is received.
		/// </summary>
		event EventHandler<ChatMessageEventArgs> MessageReceived;

		/// <summary>
		/// Raised when an existing message has been updated.
		/// </summary>
		event EventHandler<ChatMessageEventArgs> MessageUpdated;

		/// <summary>
		/// Raised when the remote party acknowledges delivery.
		/// </summary>
		event EventHandler<ChatDeliveryReceiptEventArgs> DeliveryReceiptReceived;

		/// <summary>
		/// Sends a new message and returns the identifier used by transport.
		/// </summary>
		/// <param name="Message">Outbound message.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<string> SendAsync(ChatOutboundMessage Message, CancellationToken CancellationToken);

		/// <summary>
		/// Sends a correction for an existing message.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="RemoteObjectId">Remote object id.</param>
		/// <param name="Message">Outbound message.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task SendCorrectionAsync(string RemoteBareJid, string RemoteObjectId, ChatOutboundMessage Message, CancellationToken CancellationToken);

		/// <summary>
		/// Sends a delivery acknowledgement for a received message.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="RemoteObjectId">Remote object id.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task AcknowledgeAsync(string RemoteBareJid, string RemoteObjectId, CancellationToken CancellationToken);

		/// <summary>
		/// Sends a displayed marker for a received message.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="RemoteObjectId">Remote object id.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task SendDisplayedMarkerAsync(string RemoteBareJid, string RemoteObjectId, CancellationToken CancellationToken);

		/// <summary>
		/// Ensures subscriptions and listeners are active for a chat session.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task EnsureSessionAsync(string RemoteBareJid, CancellationToken CancellationToken);

		/// <summary>
		/// Sends a chat state notification to the remote party.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="State">Chat state to send.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task SendChatStateAsync(string RemoteBareJid, ChatState State, CancellationToken CancellationToken);

		/// <summary>
		/// Returns whether chat state notifications are supported for the remote party.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		bool IsChatStateSupported(string RemoteBareJid);
	}
}
