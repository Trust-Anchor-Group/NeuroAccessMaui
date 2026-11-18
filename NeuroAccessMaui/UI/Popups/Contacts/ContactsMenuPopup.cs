using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// The menu of the contacts list: import a contact file, share contacts as a file, and see blocked people.
	/// </summary>
	public sealed class ContactsMenuPopup : ContactSheetPopup
	{
		private readonly TaskCompletionSource<ContactsMenuChoice?> result = new();

		/// <summary>
		/// Creates the menu.
		/// </summary>
		public ContactsMenuPopup()
		{
			this.Add(this.CreateHeader(ServiceRef.Localizer[nameof(AppResources.Contacts)], null));

			this.Add(CreateCard(
				this.CreateChoice("M14 3H6v18h12V7l-4-4z M14 3v4h4 M12 11v6 M9 14l3 3 3-3",
					nameof(AppResources.ContactAddImport), nameof(AppResources.ContactAddImportHint), ContactsMenuChoice.Import),
				this.CreateChoice("M12 15V3 M7 8l5-5 5 5 M5 14v6h14v-6",
					nameof(AppResources.ContactExportTitle), nameof(AppResources.ContactExportHint), ContactsMenuChoice.Export),
				this.CreateChoice("M12 3a9 9 0 1 1 0 18a9 9 0 1 1 0-18z M5.6 5.6l12.8 12.8",
					nameof(AppResources.ContactBlockedPeople), nameof(AppResources.ContactBlockedPeopleHint), ContactsMenuChoice.Blocked)));
		}

		/// <summary>
		/// Shows the menu, and waits for the user's choice.
		/// </summary>
		/// <returns>What the user chose, or null if the menu was closed.</returns>
		public static async Task<ContactsMenuChoice?> ShowAsync()
		{
			ContactsMenuPopup Sheet = new();
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}

		private View CreateChoice(string IconData, string TitleKey, string SubtitleKey, ContactsMenuChoice Choice)
		{
			return this.CreateRow(IconData, ServiceRef.Localizer[TitleKey], ServiceRef.Localizer[SubtitleKey], false, null,
				() => this.result.TrySetResult(Choice));
		}
	}
}
