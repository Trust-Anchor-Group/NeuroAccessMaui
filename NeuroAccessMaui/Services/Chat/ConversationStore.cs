using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Settings;
using ChatMessageType = NeuroAccessMaui.UI.Pages.Contacts.Chat.MessageType;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Keeps the list of chats: one <see cref="Conversation"/> for each person the user chats with, and one for the user's
	/// own notes. It follows <see cref="ChatEvents"/>, so the last message and the number of unread messages stay up to date.
	/// </summary>
	public static class ConversationStore
	{
		/// <summary>
		/// The setting that tells if the list has been built from the messages already on the phone.
		/// </summary>
		private const string builtSetting = "CHATS.CONVERSATIONS_BUILT";

		private const int pageSize = 500;

		private static readonly SemaphoreSlim updateLock = new(1, 1);
		private static int started;

		/// <summary>
		/// Raised when a conversation has been added, changed or removed.
		/// </summary>
		public static event EventHandler? Changed;

		/// <summary>
		/// Starts following chat events, and builds the list from the messages on the phone the first time. Calling it again
		/// does nothing.
		/// </summary>
		public static void Start()
		{
			if (Interlocked.Exchange(ref started, 1) == 1)
				return;

			ChatEvents.MessageStored += ChatEvents_MessageStored;
			ChatEvents.MessageDeleted += ChatEvents_MessageDeleted;

			_ = Task.Run(BuildOnceAsync);
		}

		#region Reading

		/// <summary>
		/// Gets all conversations: pinned ones first, then the one with the latest message first.
		/// </summary>
		/// <returns>The conversations.</returns>
		public static async Task<IReadOnlyList<Conversation>> GetAllAsync()
		{
			IEnumerable<Conversation> All = await Database.Find<Conversation>();
			return [.. All.OrderByDescending(C => C.Pinned).ThenByDescending(C => C.LastMessageAt)];
		}

		/// <summary>
		/// Finds the conversation with a chat address.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		/// <returns>The conversation, if there is one.</returns>
		public static async Task<Conversation?> FindAsync(string? Address)
		{
			if (string.IsNullOrEmpty(Address))
				return null;

			return await Database.FindFirstIgnoreRest<Conversation>(new FilterFieldEqualTo("Address", Address));
		}

		/// <summary>
		/// The number of unread messages in all chats that are not muted.
		/// </summary>
		/// <returns>The number of unread messages.</returns>
		public static async Task<int> GetUnreadTotalAsync()
		{
			IEnumerable<Conversation> All = await Database.Find<Conversation>();
			return All.Where(C => !C.Muted).Sum(C => C.UnreadCount);
		}

		/// <summary>
		/// If the chat with a chat address is muted, so its messages give no notification.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		/// <returns>If the chat is muted.</returns>
		public static async Task<bool> IsMutedAsync(string Address)
		{
			Conversation? Conversation = await FindAsync(Address);
			return Conversation is not null && Conversation.Muted;
		}

		/// <summary>
		/// If a chat address is the user's own, so the chat holds the user's own notes.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		/// <returns>If the address is the user's own.</returns>
		public static bool IsOwnAddress(string? Address)
		{
			string? Own = ServiceRef.XmppService.BareJid;
			return !string.IsNullOrEmpty(Own) && string.Equals(Address, Own, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region Changing

		/// <summary>
		/// Makes sure the user's own notes are in the list, pinned to the top the first time.
		/// </summary>
		public static async Task EnsureNotesAsync()
		{
			string? Own = ServiceRef.XmppService.BareJid;
			if (string.IsNullOrEmpty(Own))
				return;

			await UpdateAsync(Own, _ => false, true);
		}

		/// <summary>
		/// Marks every message in a chat as read, and removes its notifications.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		public static async Task MarkReadAsync(string Address)
		{
			await UpdateAsync(Address, C =>
			{
				if (C.UnreadCount == 0)
					return false;

				C.UnreadCount = 0;
				return true;
			}, false);

			try
			{
				await ServiceRef.NotificationService.DeleteEvents(NotificationEventType.Contacts, Address);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Keeps a chat at the top of the list, or stops keeping it there.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		/// <param name="Pinned">If the chat is kept at the top.</param>
		public static Task SetPinnedAsync(string Address, bool Pinned)
		{
			return UpdateAsync(Address, C =>
			{
				if (C.Pinned == Pinned)
					return false;

				C.Pinned = Pinned;
				return true;
			}, true);
		}

		/// <summary>
		/// Mutes a chat, so its messages give no notification, or turns notifications back on.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		/// <param name="Muted">If the chat is muted.</param>
		public static Task SetMutedAsync(string Address, bool Muted)
		{
			return UpdateAsync(Address, C =>
			{
				if (C.Muted == Muted)
					return false;

				C.Muted = Muted;
				return true;
			}, true);
		}

		/// <summary>
		/// Deletes a chat from this phone: all its messages, its notifications and its place in the list. The other person keeps
		/// their copy.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		public static async Task DeleteChatAsync(string Address)
		{
			await updateLock.WaitAsync();

			try
			{
				await Database.FindDelete<ChatMessage>(new FilterFieldEqualTo("RemoteBareJid", Address));

				Conversation? Conversation = await FindAsync(Address);
				if (Conversation is not null)
					await Database.Delete(Conversation);
			}
			finally
			{
				updateLock.Release();
			}

			try
			{
				await ServiceRef.NotificationService.DeleteEvents(NotificationEventType.Contacts, Address);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			RaiseChanged();
		}

		/// <summary>
		/// Changes the conversation with a chat address, and saves it if it changed.
		/// </summary>
		/// <param name="Address">The chat address.</param>
		/// <param name="Change">Changes the conversation, and tells if anything changed.</param>
		/// <param name="Create">If the conversation is made when there is none.</param>
		private static async Task UpdateAsync(string Address, Func<Conversation, bool> Change, bool Create)
		{
			await updateLock.WaitAsync();

			try
			{
				Conversation? Conversation = await FindAsync(Address);
				bool IsNew = Conversation is null;

				if (Conversation is null)
				{
					if (!Create)
						return;

					Conversation = await NewConversationAsync(Address);
				}

				if (!Change(Conversation) && !IsNew)
					return;

				if (IsNew)
					await Database.Insert(Conversation);
				else
					await Database.Update(Conversation);
			}
			finally
			{
				updateLock.Release();
			}

			RaiseChanged();
		}

		/// <summary>
		/// A new conversation with a chat address. The user's own notes are pinned to the top.
		/// </summary>
		private static async Task<Conversation> NewConversationAsync(string Address)
		{
			if (GroupStore.IsGroup(Address))
			{
				ChatGroup? Group = await GroupStore.FindAsync(Address);

				return new Conversation()
				{
					Kind = ConversationKind.Group,
					Address = Address,
					Title = Group?.Name ?? string.Empty
				};
			}

			bool IsOwn = IsOwnAddress(Address);

			return new Conversation()
			{
				Kind = ConversationKind.Person,
				Address = Address,
				Title = IsOwn ? string.Empty : await TitleOfAsync(Address),
				Pinned = IsOwn
			};
		}

		/// <summary>
		/// Makes sure a group has its chat in the list, with the group's current name.
		/// </summary>
		/// <param name="Address">The group's address.</param>
		/// <param name="Name">The group's name.</param>
		public static Task EnsureGroupAsync(string Address, string Name)
		{
			return UpdateAsync(Address, C =>
			{
				if (C.Kind == ConversationKind.Group && string.Equals(C.Title, Name, StringComparison.Ordinal))
					return false;

				C.Kind = ConversationKind.Group;
				C.Title = Name;
				return true;
			}, true);
		}

		/// <summary>
		/// The name of the person with a chat address: the contact's name, or else the first part of the address.
		/// </summary>
		private static async Task<string> TitleOfAsync(string Address)
		{
			string? Name = await ContactStore.TryGetNameAsync(Address);
			if (!string.IsNullOrWhiteSpace(Name))
				return Name;

			int i = Address.IndexOf('@');
			return i > 0 ? Address[..i] : Address;
		}

		#endregion

		#region Following chat events

		private static void ChatEvents_MessageStored(object? Sender, ChatMessageEventArgs e)
		{
			_ = OnMessageStoredAsync(e.Message, e.IsUpdate);
		}

		private static void ChatEvents_MessageDeleted(object? Sender, ChatMessageEventArgs e)
		{
			_ = OnMessageDeletedAsync(e.RemoteBareJid);
		}

		/// <summary>
		/// Shows a message that was sent, received or corrected as the chat's last message, if it is the latest one, and
		/// counts a received message as unread if its chat is not on screen.
		/// </summary>
		private static async Task OnMessageStoredAsync(ChatMessage Message, bool IsUpdate)
		{
			try
			{
				string Address = Message.RemoteBareJid?.Value ?? string.Empty;
				if (string.IsNullOrEmpty(Address))
					return;

				string Preview = ChatPreview.Of(Message);
				bool Unread = !IsUpdate && Message.MessageType == ChatMessageType.Received && !ChatEvents.IsChatVisible(Address);

				// In a group, the preview says who wrote, by their first name.
				if (Message.MessageType == ChatMessageType.Received && !string.IsNullOrWhiteSpace(Message.SenderName))
				{
					string Sender = Message.SenderName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
					Preview = ServiceRef.Localizer[nameof(AppResources.GroupsSenderPreview), Sender, Preview];
				}

				await UpdateAsync(Address, C =>
				{
					bool Changed = false;

					if (Message.Created >= C.LastMessageAt)
					{
						C.LastMessageAt = Message.Created;
						C.LastPreview = Preview;
						C.LastFromMe = Message.MessageType == ChatMessageType.Sent;
						Changed = true;
					}

					if (Unread)
					{
						C.UnreadCount++;
						Changed = true;
					}

					return Changed;
				}, true);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Shows the chat's latest remaining message as its last message, after a message was deleted.
		/// </summary>
		private static async Task OnMessageDeletedAsync(string Address)
		{
			try
			{
				if (string.IsNullOrEmpty(Address))
					return;

				ChatMessage? Latest = await LatestMessageAsync(Address);

				await UpdateAsync(Address, C =>
				{
					C.LastMessageAt = Latest?.Created ?? C.LastMessageAt;
					C.LastPreview = Latest is null ? string.Empty : ChatPreview.Of(Latest);
					C.LastFromMe = Latest is not null && Latest.MessageType == ChatMessageType.Sent;
					return true;
				}, false);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private static async Task<ChatMessage?> LatestMessageAsync(string Address)
		{
			IEnumerable<ChatMessage> Found = await Database.Find<ChatMessage>(0, 1, new FilterFieldEqualTo("RemoteBareJid", Address), "-Created");
			return Found.FirstOrDefault();
		}

		#endregion

		#region Building the list the first time

		/// <summary>
		/// Builds the list from the messages already on the phone, the first time the app runs with it. Each chat gets its
		/// latest message, and no unread messages.
		/// </summary>
		private static async Task BuildOnceAsync()
		{
			try
			{
				if (await RuntimeSettings.GetAsync(builtSetting, false))
					return;

				Dictionary<string, ChatMessage> Latest = new(StringComparer.OrdinalIgnoreCase);
				int Offset = 0;

				while (true)
				{
					IEnumerable<ChatMessage> Page = await Database.Find<ChatMessage>(Offset, pageSize, "RemoteBareJid", "Created");
					int Count = 0;

					foreach (ChatMessage Message in Page)
					{
						Count++;

						string Address = Message.RemoteBareJid?.Value ?? string.Empty;
						if (string.IsNullOrEmpty(Address))
							continue;

						if (!Latest.TryGetValue(Address, out ChatMessage? Known) || Message.Created >= Known.Created)
							Latest[Address] = Message;
					}

					if (Count < pageSize)
						break;

					Offset += Count;
				}

				foreach (KeyValuePair<string, ChatMessage> P in Latest)
				{
					string Preview = ChatPreview.Of(P.Value);

					await UpdateAsync(P.Key, C =>
					{
						if (P.Value.Created < C.LastMessageAt)
							return false;

						C.LastMessageAt = P.Value.Created;
						C.LastPreview = Preview;
						C.LastFromMe = P.Value.MessageType == ChatMessageType.Sent;
						return true;
					}, true);
				}

				await EnsureNotesAsync();
				await RuntimeSettings.SetAsync(builtSetting, true);

				ServiceRef.LogService.LogInformational("ConversationsBuilt", new KeyValuePair<string, object?>("Count", Latest.Count));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion

		private static void RaiseChanged()
		{
			try
			{
				Changed?.Invoke(null, EventArgs.Empty);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
