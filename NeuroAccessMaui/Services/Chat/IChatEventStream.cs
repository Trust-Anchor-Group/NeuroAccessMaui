using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Events;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Provides event queuing and batching for chat sessions.
	/// </summary>
	public interface IChatEventStream
	{
		/// <summary>
		/// Raised when events become available for consumption.
		/// </summary>
		event EventHandler<ChatEventsAvailableEventArgs> EventsAvailable;

		/// <summary>
		/// Publishes a new event into the stream.
		/// </summary>
		/// <param name="Event">Event to publish.</param>
		void Publish(ChatSessionEvent Event);

		/// <summary>
		/// Drains all pending events for the specified session.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<IReadOnlyList<ChatSessionEvent>> DrainAsync(string RemoteBareJid, CancellationToken CancellationToken);

		/// <summary>
		/// Clears queued events for a session.
		/// </summary>
		/// <param name="RemoteBareJid">Remote bare JID.</param>
		void Clear(string RemoteBareJid);
	}
}
