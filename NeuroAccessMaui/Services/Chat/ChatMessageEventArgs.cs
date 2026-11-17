using NeuroAccessMaui.UI.Pages.Contacts.Chat;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// A chat message that was stored or changed.
	/// </summary>
	/// <param name="Message">The message.</param>
	/// <param name="IsUpdate">If the message corrects an earlier message, which it replaces.</param>
	public sealed class ChatMessageEventArgs(ChatMessage Message, bool IsUpdate) : EventArgs
	{
		/// <summary>
		/// The message.
		/// </summary>
		public ChatMessage Message { get; } = Message;

		/// <summary>
		/// If the message corrects an earlier message, which it replaces.
		/// </summary>
		public bool IsUpdate { get; } = IsUpdate;

		/// <summary>
		/// Address of the other party in the chat.
		/// </summary>
		public string RemoteBareJid => this.Message.RemoteBareJid?.Value ?? string.Empty;
	}
}
