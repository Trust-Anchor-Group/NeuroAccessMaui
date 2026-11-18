using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.Export
{
	/// <summary>
	/// View model of the page that exports contacts to a contact file. The user chooses the contacts, and whether photos and
	/// notes go with them, and shares one .vcf file.
	/// </summary>
	/// <param name="Args">Navigation arguments, with the contacts to choose to start with, if any.</param>
	public partial class ExportContactsViewModel(ExportContactsNavigationArgs? Args) : BaseViewModel
	{
		private readonly ExportContactsNavigationArgs? args = Args;

		/// <summary>
		/// The contacts.
		/// </summary>
		public ObservableCollection<ExportContactItem> Items { get; } = [];

		/// <summary>
		/// If the contacts are being loaded.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasContacts))]
		[NotifyPropertyChangedFor(nameof(IsEmpty))]
		private bool isLoading = true;

		/// <summary>
		/// If the file is being made and shared.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ShareCommand))]
		private bool isSharing;

		/// <summary>
		/// If photos go with the contacts.
		/// </summary>
		[ObservableProperty]
		private bool includePhotos = true;

		/// <summary>
		/// If the user's notes go with the contacts.
		/// </summary>
		[ObservableProperty]
		private bool includeNotes;

		/// <summary>
		/// The number of contacts chosen.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ShareCommand))]
		private int selectedCount;

		/// <summary>
		/// The text of the share button, with the number of chosen contacts.
		/// </summary>
		[ObservableProperty]
		private string shareText = string.Empty;

		/// <summary>
		/// The text of the button that chooses all contacts, or none when all are chosen.
		/// </summary>
		[ObservableProperty]
		private string selectAllText = string.Empty;

		/// <summary>
		/// If there are contacts to export.
		/// </summary>
		public bool HasContacts => !this.IsLoading && this.Items.Count > 0;

		/// <summary>
		/// If the user has no contacts.
		/// </summary>
		public bool IsEmpty => !this.IsLoading && this.Items.Count == 0;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			try
			{
				IReadOnlyList<Contact> Contacts = await ContactStore.GetAllAsync();
				HashSet<string>? Chosen = this.args?.ObjectIds is null ? null : new(this.args.ObjectIds, StringComparer.Ordinal);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Items.Clear();

					foreach (Contact Contact in Contacts)
					{
						ExportContactItem Item = new(Contact, this.UpdateSelection);

						if (Chosen is not null)
							Item.IsSelected = Chosen.Contains(Contact.ObjectId ?? string.Empty);

						this.Items.Add(Item);
					}

					this.IsLoading = false;
					this.UpdateSelection();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Counts the chosen contacts, and updates the buttons.
		/// </summary>
		private void UpdateSelection()
		{
			int Count = this.Items.Count(Item => Item.IsSelected);

			this.SelectedCount = Count;
			this.ShareText = Count == 1 ?
				ServiceRef.Localizer[nameof(AppResources.ContactExportShareOne)] :
				ServiceRef.Localizer[nameof(AppResources.ContactExportShareMany), Count];
			this.SelectAllText = Count == this.Items.Count && Count > 0 ?
				ServiceRef.Localizer[nameof(AppResources.ContactExportSelectNone)] :
				ServiceRef.Localizer[nameof(AppResources.ContactExportSelectAll)];

			this.OnPropertyChanged(nameof(this.HasContacts));
			this.OnPropertyChanged(nameof(this.IsEmpty));
		}

		/// <summary>
		/// Chooses all contacts, or none when all are chosen.
		/// </summary>
		[RelayCommand]
		private void SelectAll()
		{
			bool Select = this.SelectedCount < this.Items.Count;

			foreach (ExportContactItem Item in this.Items)
				Item.IsSelected = Select;
		}

		private bool CanShare()
		{
			return this.SelectedCount > 0 && !this.IsSharing;
		}

		/// <summary>
		/// Writes the chosen contacts to one contact file, and shares it.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanShare))]
		private async Task ShareAsync()
		{
			this.IsSharing = true;

			try
			{
				List<Contact> Chosen = [.. this.Items.Where(Item => Item.IsSelected).Select(Item => Item.Contact)];
				bool Photos = this.IncludePhotos;
				bool Notes = this.IncludeNotes;

				string Text = await Task.Run(() => ContactStore.Export(Chosen, Photos, Notes));
				string FileName = Chosen.Count == 1 ? SafeFileName(Chosen[0].DisplayName) + ".vcf" : "contacts.vcf";
				string FilePath = Path.Combine(FileSystem.CacheDirectory, FileName);

				await File.WriteAllTextAsync(FilePath, Text, new UTF8Encoding(false));

				await Share.Default.RequestAsync(new ShareFileRequest()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.ContactExportTitle)],
					File = new ShareFile(FilePath, "text/vcard")
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsSharing = false;
			}
		}

		/// <summary>
		/// A file name made from a contact's name, without characters that file names can't have.
		/// </summary>
		private static string SafeFileName(string Name)
		{
			char[] Invalid = Path.GetInvalidFileNameChars();
			string Result = new([.. Name.Trim().Select(Ch => Array.IndexOf(Invalid, Ch) >= 0 ? '_' : Ch)]);

			return string.IsNullOrWhiteSpace(Result) ? "contact" : Result;
		}
	}
}
