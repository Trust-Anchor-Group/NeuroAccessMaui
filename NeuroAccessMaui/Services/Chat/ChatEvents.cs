using NeuroAccessMaui.UI.Pages.Contacts.Chat;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Passes chat events, from the connection and from sending, to the chat pages that are open, and keeps track of the chat
	/// on screen and of the parties that use chat states.
	/// </summary>
	/// <remarks>
	/// Events are raised on the thread that caused them. Handlers that change the UI move to the main thread themselves.
	/// </remarks>
	public static class ChatEvents
	{
		private static readonly object synch = new();
		private static readonly HashSet<string> chatStatePeers = new(StringComparer.OrdinalIgnoreCase);
		private static string? visibleBareJid;

		/// <summary>
		/// Raised when a chat message has been stored, after it was sent or received, or corrected.
		/// </summary>
		public static event EventHandler<ChatMessageEventArgs>? MessageStored;

		/// <summary>
		/// Raised when the other party has confirmed that a sent message arrived.
		/// </summary>
		public static event EventHandler<ChatMessageEventArgs>? MessageDelivered;

		/// <summary>
		/// Raised when a chat message has been deleted from the phone.
		/// </summary>
		public static event EventHandler<ChatMessageEventArgs>? MessageDeleted;

		/// <summary>
		/// Raised when the other party in a chat starts or stops writing.
		/// </summary>
		public static event EventHandler<ChatStateEventArgs>? ChatStateChanged;

		/// <summary>
		/// Notes that the chat with <paramref name="BareJid"/> is on screen, so its messages need no notification.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		public static void SetVisibleChat(string BareJid)
		{
			lock (synch)
			{
				visibleBareJid = BareJid;
			}
		}

		/// <summary>
		/// Notes that the chat with <paramref name="BareJid"/> is no longer on screen.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		public static void ClearVisibleChat(string BareJid)
		{
			lock (synch)
			{
				if (string.Equals(visibleBareJid, BareJid, StringComparison.OrdinalIgnoreCase))
					visibleBareJid = null;
			}
		}

		/// <summary>
		/// If the chat with <paramref name="BareJid"/> is on screen.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		/// <returns>If the chat is on screen.</returns>
		public static bool IsChatVisible(string BareJid)
		{
			lock (synch)
			{
				return string.Equals(visibleBareJid, BareJid, StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Notes that <paramref name="BareJid"/> sends chat states, so it is told when the user writes.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		public static void AddChatStatePeer(string BareJid)
		{
			lock (synch)
			{
				chatStatePeers.Add(BareJid);
			}
		}

		/// <summary>
		/// If <paramref name="BareJid"/> has sent chat states, and so understands them.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		/// <returns>If chat states are used.</returns>
		public static bool UsesChatStates(string BareJid)
		{
			lock (synch)
			{
				return chatStatePeers.Contains(BareJid);
			}
		}

		/// <summary>
		/// Raises <see cref="MessageStored"/>.
		/// </summary>
		/// <param name="Message">The stored message.</param>
		/// <param name="IsUpdate">If the message corrects an earlier message.</param>
		public static void OnMessageStored(ChatMessage Message, bool IsUpdate)
		{
			Raise(MessageStored, new ChatMessageEventArgs(Message, IsUpdate));
		}

		/// <summary>
		/// Raises <see cref="MessageDelivered"/>.
		/// </summary>
		/// <param name="Message">The delivered message.</param>
		public static void OnMessageDelivered(ChatMessage Message)
		{
			Raise(MessageDelivered, new ChatMessageEventArgs(Message, true));
		}

		/// <summary>
		/// Raises <see cref="MessageDeleted"/>.
		/// </summary>
		/// <param name="Message">The deleted message.</param>
		public static void OnMessageDeleted(ChatMessage Message)
		{
			Raise(MessageDeleted, new ChatMessageEventArgs(Message, false));
		}

		/// <summary>
		/// Raises <see cref="ChatStateChanged"/>.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		/// <param name="State">What the other party is doing.</param>
		public static void OnChatState(string BareJid, ChatState State)
		{
			Raise(ChatStateChanged, new ChatStateEventArgs(BareJid, State));
		}

		/// <summary>
		/// Calls each handler on its own, so that one that fails does not stop the others.
		/// </summary>
		private static void Raise<T>(EventHandler<T>? Handlers, T e)
		{
			if (Handlers is null)
				return;

			foreach (Delegate Handler in Handlers.GetInvocationList())
			{
				try
				{
					((EventHandler<T>)Handler)(null, e);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}
		}
	}
}
