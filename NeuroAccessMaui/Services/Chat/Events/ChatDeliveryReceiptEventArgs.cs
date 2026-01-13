using System;
using NeuroAccessMaui.Services.Chat.Models;

namespace NeuroAccessMaui.Services.Chat.Events
{
	/// <summary>
	/// Event data for delivery receipt notifications.
	/// </summary>
	public class ChatDeliveryReceiptEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatDeliveryReceiptEventArgs"/> class.
		/// </summary>
		/// <param name="remoteBareJid">Remote bare JID.</param>
		/// <param name="messageId">Local message identifier.</param>
		/// <param name="deliveryStatus">Delivery status.</param>
		/// <param name="timestamp">Timestamp associated with the receipt.</param>
		public ChatDeliveryReceiptEventArgs(string remoteBareJid, string messageId, ChatDeliveryStatus deliveryStatus, DateTime timestamp)
		{
			this.RemoteBareJid = remoteBareJid ?? throw new ArgumentNullException(nameof(remoteBareJid));
			this.MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
			this.DeliveryStatus = deliveryStatus;
			this.Timestamp = timestamp;
		}

		/// <summary>
		/// Remote bare JID.
		/// </summary>
		public string RemoteBareJid { get; }

		/// <summary>
		/// Local message identifier.
		/// </summary>
		public string MessageId { get; }

		/// <summary>
		/// Delivery status.
		/// </summary>
		public ChatDeliveryStatus DeliveryStatus { get; }

		/// <summary>
		/// Timestamp associated with the receipt.
		/// </summary>
		public DateTime Timestamp { get; }
	}
}
