namespace NeuroAccessMaui.UI.Popups.Chats
{
	/// <summary>
	/// What the user chose to do with a chat in the list of chats.
	/// </summary>
	public enum ChatAction
	{
		/// <summary>
		/// Keep the chat at the top of the list.
		/// </summary>
		Pin,

		/// <summary>
		/// Stop keeping the chat at the top of the list.
		/// </summary>
		Unpin,

		/// <summary>
		/// Give no notifications for new messages in the chat.
		/// </summary>
		Mute,

		/// <summary>
		/// Give notifications for new messages in the chat again.
		/// </summary>
		Unmute,

		/// <summary>
		/// Mark every message in the chat as read.
		/// </summary>
		MarkRead,

		/// <summary>
		/// Delete the chat from this phone.
		/// </summary>
		Delete
	}
}
