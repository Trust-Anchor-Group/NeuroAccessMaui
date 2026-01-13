using System;

namespace NeuroAccessMaui.Services.Chat.Events
{
	/// <summary>
	/// Event data raised when chat session events become available.
	/// </summary>
	public class ChatEventsAvailableEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatEventsAvailableEventArgs"/> class.
		/// </summary>
		/// <param name="remoteBareJid">Remote bare JID with pending events.</param>
		public ChatEventsAvailableEventArgs(string remoteBareJid)
		{
			this.RemoteBareJid = remoteBareJid ?? throw new ArgumentNullException(nameof(remoteBareJid));
		}

		/// <summary>
		/// Remote bare JID with pending events.
		/// </summary>
		public string RemoteBareJid { get; }
	}
}
