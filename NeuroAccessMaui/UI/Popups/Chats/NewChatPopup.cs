using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Contacts;

namespace NeuroAccessMaui.UI.Popups.Chats
{
	/// <summary>
	/// What the New button in Chats starts: a chat with one person, or a new group.
	/// </summary>
	public sealed class NewChatPopup : ContactSheetPopup
	{
		private const string personIcon = "M12 4a4 4 0 1 1 0 8a4 4 0 1 1 0-8z M4 21a8 8 0 0 1 16 0";
		private const string groupIcon = "M9 4.5a3.5 3.5 0 1 1 0 7a3.5 3.5 0 1 1 0-7z M2.5 20a6.5 6.5 0 0 1 13 0 M17 6.5a2.5 2.5 0 1 1 0 5a2.5 2.5 0 1 1 0-5z M16 14.5a5 5 0 0 1 6 4.5";

		private readonly TaskCompletionSource<NewChatChoice?> result = new();

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		public NewChatPopup()
		{
			this.Add(this.CreateHeader(ServiceRef.Localizer[nameof(AppResources.ChatsNew)], null));

			this.Add(CreateCard(
				this.CreateChoice(personIcon, nameof(AppResources.GroupsNewChat), NewChatChoice.Chat),
				this.CreateChoice(groupIcon, nameof(AppResources.GroupsNewGroup), NewChatChoice.Group)));
		}

		/// <summary>
		/// Shows the sheet, and waits for the user's choice.
		/// </summary>
		/// <returns>What the user chose, or null if the sheet was closed.</returns>
		public static async Task<NewChatChoice?> ShowAsync()
		{
			NewChatPopup Sheet = new();
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}

		private View CreateChoice(string IconData, string TitleKey, NewChatChoice Choice)
		{
			return this.CreateRow(IconData, ServiceRef.Localizer[TitleKey], null, false, null, () => this.result.TrySetResult(Choice));
		}
	}
}
