using System;
using NeuroAccessMaui.Services.Chat.Models;

namespace NeuroAccessMaui.Services.Chat.Events
{
	/// <summary>
	/// Event data for inbound or outbound chat message notifications.
	/// </summary>
	public class ChatMessageEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMessageEventArgs"/> class.
		/// </summary>
		/// <param name="descriptor">Message descriptor.</param>
		public ChatMessageEventArgs(ChatMessageDescriptor descriptor)
		{
			this.MessageDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
		}

		/// <summary>
		/// Descriptor for the message related to the event.
		/// </summary>
		public ChatMessageDescriptor MessageDescriptor { get; }
	}
}
