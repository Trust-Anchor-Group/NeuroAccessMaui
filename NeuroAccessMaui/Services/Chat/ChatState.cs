namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// What a party in a chat is doing, as told by chat state notifications (XEP-0085).
	/// </summary>
	public enum ChatState
	{
		/// <summary>
		/// Takes part in the chat.
		/// </summary>
		Active,

		/// <summary>
		/// Is writing a message.
		/// </summary>
		Composing,

		/// <summary>
		/// Has stopped writing for a moment.
		/// </summary>
		Paused,

		/// <summary>
		/// Is not looking at the chat.
		/// </summary>
		Inactive,

		/// <summary>
		/// Has left the chat.
		/// </summary>
		Gone
	}
}
