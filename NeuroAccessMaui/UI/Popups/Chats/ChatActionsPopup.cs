using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Contacts;

namespace NeuroAccessMaui.UI.Popups.Chats
{
	/// <summary>
	/// What can be done with a chat in the list of chats, shown on a long press: pin or unpin it, mute or unmute it, mark it
	/// as read, or delete it from this phone.
	/// </summary>
	public sealed class ChatActionsPopup : ContactSheetPopup
	{
		private const string pinIcon = "M9 4h6l-1 6 3 3H7l3-3-1-6z M12 16v5";
		private const string muteIcon = "M6 16V11a6 6 0 0 1 9.5-4.9M18 11v5l2 2H8 M3 3l18 18";
		private const string bellIcon = "M6 16v-5a6 6 0 0 1 12 0v5l2 2H4z M10 21h4";
		private const string readIcon = "M2 12l5 5L17 7 M12 16l1 1L23 7";
		private const string deleteIcon = "M4 7h16 M10 11v6 M14 11v6 M6 7l1 13h10l1-13 M9 7V4h6v3";

		private readonly TaskCompletionSource<ChatAction?> result = new();

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		/// <param name="Title">The name of the chat.</param>
		/// <param name="IsPinned">If the chat is pinned.</param>
		/// <param name="IsMuted">If the chat is muted.</param>
		/// <param name="HasUnread">If the chat has unread messages.</param>
		public ChatActionsPopup(string Title, bool IsPinned, bool IsMuted, bool HasUnread)
		{
			this.Add(this.CreateHeader(Title, null));

			List<View> Rows =
			[
				IsPinned ?
					this.CreateChoice(pinIcon, nameof(AppResources.ChatsUnpin), null, false, ChatAction.Unpin) :
					this.CreateChoice(pinIcon, nameof(AppResources.ChatsPin), null, false, ChatAction.Pin),
				IsMuted ?
					this.CreateChoice(bellIcon, nameof(AppResources.ChatsUnmute), null, false, ChatAction.Unmute) :
					this.CreateChoice(muteIcon, nameof(AppResources.ChatsMute), nameof(AppResources.ChatsMuteHint), false, ChatAction.Mute)
			];

			if (HasUnread)
				Rows.Add(this.CreateChoice(readIcon, nameof(AppResources.ChatsMarkRead), null, false, ChatAction.MarkRead));

			this.Add(CreateCard([.. Rows]));
			this.Add(CreateCard(this.CreateChoice(deleteIcon, nameof(AppResources.ChatsDelete), nameof(AppResources.ChatsDeleteHint), true,
				ChatAction.Delete)));
		}

		/// <summary>
		/// Shows the sheet, and waits for the user's choice.
		/// </summary>
		/// <param name="Title">The name of the chat.</param>
		/// <param name="IsPinned">If the chat is pinned.</param>
		/// <param name="IsMuted">If the chat is muted.</param>
		/// <param name="HasUnread">If the chat has unread messages.</param>
		/// <returns>What the user chose, or null if the sheet was closed.</returns>
		public static async Task<ChatAction?> ShowAsync(string Title, bool IsPinned, bool IsMuted, bool HasUnread)
		{
			ChatActionsPopup Sheet = new(Title, IsPinned, IsMuted, HasUnread);
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}

		private View CreateChoice(string IconData, string TitleKey, string? SubtitleKey, bool Danger, ChatAction Action)
		{
			string? Subtitle = SubtitleKey is null ? null : ServiceRef.Localizer[SubtitleKey].Value;

			return this.CreateRow(IconData, ServiceRef.Localizer[TitleKey], Subtitle, Danger, null, () => this.result.TrySetResult(Action));
		}
	}
}
