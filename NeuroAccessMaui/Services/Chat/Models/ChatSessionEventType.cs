namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Types of events flowing through the chat event stream.
	/// </summary>
	public enum ChatSessionEventType
	{
		/// <summary>
		/// One or more messages were added.
		/// </summary>
		MessagesAppended,

		/// <summary>
		/// Existing message has been updated.
		/// </summary>
		MessageUpdated,

		/// <summary>
		/// Delivery receipt update.
		/// </summary>
		DeliveryReceipt,

		/// <summary>
		/// Typing indicator update.
		/// </summary>
		TypingState,

		/// <summary>
		/// Session level status update (connectivity, presence).
		/// </summary>
		SessionState
	}
}
