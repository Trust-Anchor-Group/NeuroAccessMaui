using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI.Toasts;

namespace NeuroAccessMaui.UI.Pages.Contacts.Import
{
	/// <summary>
	/// View model of the page that imports a contact file. It shows the cards in the file, what importing each one does, and
	/// imports those the user keeps chosen. Nothing in the file is trusted: imported contacts are never marked as verified.
	/// </summary>
	/// <param name="Args">Navigation arguments, with the file's text if it has been read already.</param>
	public partial class ImportContactsViewModel(ImportContactsNavigationArgs? Args) : BaseViewModel
	{
		private readonly ImportContactsNavigationArgs? args = Args;
		private bool started;

		/// <summary>
		/// The cards in the file.
		/// </summary>
		public ObservableCollection<ImportContactItem> Items { get; } = [];

		/// <summary>
		/// If the file is being read.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasCards))]
		private bool isReading = true;

		/// <summary>
		/// If the cards are being imported.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ImportCommand))]
		private bool isImporting;

		/// <summary>
		/// The name of the file.
		/// </summary>
		[ObservableProperty]
		private string fileName = string.Empty;

		/// <summary>
		/// How many contacts the file has, in words.
		/// </summary>
		[ObservableProperty]
		private string foundText = string.Empty;

		/// <summary>
		/// How many cards are new contacts, in words.
		/// </summary>
		[ObservableProperty]
		private string newText = string.Empty;

		/// <summary>
		/// How many cards update contacts, in words.
		/// </summary>
		[ObservableProperty]
		private string updatedText = string.Empty;

		/// <summary>
		/// How many cards have nothing new, in words.
		/// </summary>
		[ObservableProperty]
		private string unchangedText = string.Empty;

		/// <summary>
		/// If some cards are new contacts.
		/// </summary>
		[ObservableProperty]
		private bool hasNew;

		/// <summary>
		/// If some cards update contacts.
		/// </summary>
		[ObservableProperty]
		private bool hasUpdated;

		/// <summary>
		/// If some cards have nothing new.
		/// </summary>
		[ObservableProperty]
		private bool hasUnchanged;

		/// <summary>
		/// How many cards in the file were skipped because they have no chat address.
		/// </summary>
		[ObservableProperty]
		private string skippedText = string.Empty;

		/// <summary>
		/// If any cards in the file were skipped because they have no chat address.
		/// </summary>
		[ObservableProperty]
		private bool hasSkipped;

		/// <summary>
		/// The text of the import button, with the number of chosen cards.
		/// </summary>
		[ObservableProperty]
		private string importText = string.Empty;

		/// <summary>
		/// The number of cards chosen to import.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ImportCommand))]
		private int selectedCount;

		/// <summary>
		/// If the file has been read and has cards.
		/// </summary>
		public bool HasCards => !this.IsReading && this.Items.Count > 0;

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.started)
				return;

			this.started = true;

			if (this.args?.Text is string Text)
				await this.ShowFileAsync(this.args.FileName ?? string.Empty, Text);
			else
				await this.PickFileAsync();
		}

		/// <summary>
		/// Asks the user to pick a contact file, and reads it. If the user cancels, the page closes.
		/// </summary>
		private async Task PickFileAsync()
		{
			try
			{
				FileResult? Picked = await FilePicker.Default.PickAsync(new PickOptions()
				{
					PickerTitle = ServiceRef.Localizer[nameof(AppResources.ContactImportPickTitle)],
					FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>()
					{
						{ DevicePlatform.Android, ["text/vcard", "text/x-vcard", "text/directory", "text/plain"] },
						{ DevicePlatform.iOS, ["public.vcard"] },
						{ DevicePlatform.MacCatalyst, ["public.vcard"] },
						{ DevicePlatform.WinUI, [".vcf"] }
					})
				});

				if (Picked is null)
				{
					await this.GoBack();
					return;
				}

				string Text;

				using (Stream Input = await Picked.OpenReadAsync())
				{
					if (Input.CanSeek && Input.Length > ContactStore.MaxImportLength * 4L)
					{
						await this.ShowProblemAsync(nameof(AppResources.ContactImportTooLarge));
						return;
					}

					using StreamReader Reader = new(Input, Encoding.UTF8, true);
					Text = await Reader.ReadToEndAsync();
				}

				if (Text.Length > ContactStore.MaxImportLength)
				{
					await this.ShowProblemAsync(nameof(AppResources.ContactImportTooLarge));
					return;
				}

				await this.ShowFileAsync(Picked.FileName, Text);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
				await this.GoBack();
			}
		}

		/// <summary>
		/// Reads the cards in a file, and shows what importing each one does.
		/// </summary>
		private async Task ShowFileAsync(string Name, string Text)
		{
			this.IsReading = true;

			try
			{
				ContactImportPreview Preview = await Task.Run(() => ContactStore.PreviewImportAsync(Text));
				IReadOnlyList<ContactImportItem> Found = Preview.Items;

				if (Found.Count == 0)
				{
					await this.ShowProblemAsync(Preview.SkippedCount > 0 ?
						nameof(AppResources.ContactImportNoChatAddresses) :
						nameof(AppResources.ContactImportNoContacts));
					return;
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.FileName = Name;
					this.FoundText = Found.Count == 1 ?
						ServiceRef.Localizer[nameof(AppResources.ContactImportFoundOne)] :
						ServiceRef.Localizer[nameof(AppResources.ContactImportFoundMany), Found.Count];

					int New = Found.Count(Item => Item.Kind == ContactImportKind.New);
					int Updated = Found.Count(Item => Item.Kind == ContactImportKind.Update);
					int Unchanged = Found.Count(Item => Item.Kind == ContactImportKind.Unchanged);

					this.NewText = ServiceRef.Localizer[nameof(AppResources.ContactImportNewCount), New];
					this.UpdatedText = ServiceRef.Localizer[nameof(AppResources.ContactImportUpdatedCount), Updated];
					this.UnchangedText = ServiceRef.Localizer[nameof(AppResources.ContactImportUnchangedCount), Unchanged];
					this.HasNew = New > 0;
					this.HasUpdated = Updated > 0;
					this.HasUnchanged = Unchanged > 0;
					this.SkippedText = Preview.SkippedCount == 1 ?
						ServiceRef.Localizer[nameof(AppResources.ContactImportSkippedOne)] :
						ServiceRef.Localizer[nameof(AppResources.ContactImportSkippedMany), Preview.SkippedCount];
					this.HasSkipped = Preview.SkippedCount > 0;

					this.Items.Clear();
					foreach (ContactImportItem Item in Found)
						this.Items.Add(new ImportContactItem(Item, this.UpdateSelection));

					this.IsReading = false;
					this.UpdateSelection();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
				await this.GoBack();
			}
		}

		/// <summary>
		/// Tells the user why the file can't be imported, and closes the page.
		/// </summary>
		private async Task ShowProblemAsync(string MessageKey)
		{
			await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ServiceRef.Localizer[MessageKey]);
			await this.GoBack();
		}

		/// <summary>
		/// Counts the chosen cards, and updates the import button.
		/// </summary>
		private void UpdateSelection()
		{
			int Count = this.Items.Count(Item => Item.IsSelected);

			this.SelectedCount = Count;
			this.ImportText = Count == 1 ?
				ServiceRef.Localizer[nameof(AppResources.ContactImportOne)] :
				ServiceRef.Localizer[nameof(AppResources.ContactImportMany), Count];
			this.OnPropertyChanged(nameof(this.HasCards));
		}

		private bool CanImport()
		{
			return this.SelectedCount > 0 && !this.IsImporting;
		}

		/// <summary>
		/// Imports the chosen cards, and closes the page.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanImport))]
		private async Task ImportAsync()
		{
			this.IsImporting = true;

			try
			{
				List<ContactImportItem> Chosen = [.. this.Items.Where(Item => Item.IsSelected).Select(Item => Item.Item)];
				int Count = await ContactStore.ImportAsync(Chosen);

				await ServiceRef.ToastService.ShowTextAsync(Count == 1 ?
					ServiceRef.Localizer[nameof(AppResources.ContactImportDoneOne)] :
					ServiceRef.Localizer[nameof(AppResources.ContactImportDoneMany), Count]);

				await this.GoBack();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsImporting = false;
			}
		}
	}
}
