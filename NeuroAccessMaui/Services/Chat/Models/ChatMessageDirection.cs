namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Indicates the direction or origin of a chat message.
	/// </summary>
	public enum ChatMessageDirection
	{
		/// <summary>
		/// Authenticated local user produced the message.
		/// </summary>
		Outgoing,

		/// <summary>
		/// Remote counterparty produced the message.
		/// </summary>
		Incoming,

		/// <summary>
		/// System generated informational message.
		/// </summary>
		System
	}
}
