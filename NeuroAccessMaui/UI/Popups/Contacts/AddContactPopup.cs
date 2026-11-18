using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// A sheet with the ways to add a contact: scan their code, show your own code, import a contact file, or type it in.
	/// </summary>
	public sealed class AddContactPopup : ContactSheetPopup
	{
		private readonly TaskCompletionSource<AddContactChoice?> result = new();

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		public AddContactPopup()
		{
			this.Add(this.CreateHeader(ServiceRef.Localizer[nameof(AppResources.ContactAddTitle)],
				ServiceRef.Localizer[nameof(AppResources.ContactAddSubtitle)]));

			this.Add(CreateCard(
				this.CreateChoice("M4 8V5a1 1 0 0 1 1-1h3M16 4h3a1 1 0 0 1 1 1v3M20 16v3a1 1 0 0 1-1 1h-3M8 20H5a1 1 0 0 1-1-1v-3M4 12h16",
					nameof(AppResources.ContactAddScan), nameof(AppResources.ContactAddScanHint), AddContactChoice.ScanCode),
				this.CreateChoice("M4 3h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z M15 3h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1h-5a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z M4 14h5a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-5a1 1 0 0 1 1-1z M14 14h3v3h-3z M20 14v1 M14 20h1 M18 18h3v3",
					nameof(AppResources.ContactAddShowCode), nameof(AppResources.ContactAddShowCodeHint), AddContactChoice.ShowMyCode),
				this.CreateChoice("M14 3H6v18h12V7l-4-4z M14 3v4h4 M12 11v6 M9 14l3 3 3-3",
					nameof(AppResources.ContactAddImport), nameof(AppResources.ContactAddImportHint), AddContactChoice.ImportFile),
				this.CreateChoice("M4 20h4L19 9l-4-4L4 16v4z",
					nameof(AppResources.ContactAddTypeIn), nameof(AppResources.ContactAddTypeInHint), AddContactChoice.TypeIn)));

			Label Note = CreateText(ServiceRef.Localizer[nameof(AppResources.ContactAddNote)], 14, true);
			Note.Margin = new Thickness(4, 0);
			this.Add(Note);
		}

		/// <summary>
		/// Shows the sheet, and waits for the user's choice.
		/// </summary>
		/// <returns>How the user chose to add a contact, or null if the sheet was closed.</returns>
		public static async Task<AddContactChoice?> ShowAsync()
		{
			AddContactPopup Sheet = new();
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}

		private View CreateChoice(string IconData, string TitleKey, string SubtitleKey, AddContactChoice Choice)
		{
			return this.CreateRow(IconData, ServiceRef.Localizer[TitleKey], ServiceRef.Localizer[SubtitleKey], false, null,
				() => this.result.TrySetResult(Choice));
		}
	}
}
