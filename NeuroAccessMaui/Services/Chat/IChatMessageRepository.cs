using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Abstraction for retrieving and persisting chat messages.
	/// </summary>
	public interface IChatMessageRepository
	{
		/// <summary>
		/// Loads the most recent messages for a chat session.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="PageSize">Page size.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<IReadOnlyList<ChatMessageDescriptor>> LoadRecentAsync(string RemoteBareJid, int PageSize, CancellationToken CancellationToken);

		/// <summary>
		/// Loads messages older than the provided anchor.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="Before">Timestamp anchor.</param>
		/// <param name="PageSize">Page size.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<IReadOnlyList<ChatMessageDescriptor>> LoadOlderAsync(string RemoteBareJid, DateTime Before, int PageSize, CancellationToken CancellationToken);

		/// <summary>
		/// Inserts a message into the repository.
		/// </summary>
		/// <param name="Descriptor">Message descriptor.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task SaveAsync(ChatMessageDescriptor Descriptor, CancellationToken CancellationToken);

		/// <summary>
		/// Replaces an existing message record.
		/// </summary>
		/// <param name="Descriptor">Message descriptor.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task ReplaceAsync(ChatMessageDescriptor Descriptor, CancellationToken CancellationToken);

		/// <summary>
		/// Updates the delivery status for a message.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="MessageId">Message identifier.</param>
		/// <param name="DeliveryStatus">Delivery status.</param>
		/// <param name="Timestamp">Timestamp.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task UpdateDeliveryStatusAsync(string RemoteBareJid, string MessageId, ChatDeliveryStatus DeliveryStatus, DateTime Timestamp, CancellationToken CancellationToken);

		/// <summary>
		/// Retrieves a message by id.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="MessageId">Message identifier or temp id.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<ChatMessageDescriptor?> GetAsync(string RemoteBareJid, string MessageId, CancellationToken CancellationToken);

		/// <summary>
		/// Deletes a message.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="MessageId">Message identifier.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task DeleteAsync(string RemoteBareJid, string MessageId, CancellationToken CancellationToken);
	}
}
