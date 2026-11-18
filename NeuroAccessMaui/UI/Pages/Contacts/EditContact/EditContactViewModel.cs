using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contacts.VCard;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Pages.Contacts.ContactCard;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.EditContact
{
	/// <summary>
	/// The view model of the page where a contact is typed in or changed: the chat address of a new contact, and the name,
	/// phone numbers, email addresses, address, organization, role, birthday, notes and labels. What else the contact card
	/// holds, such as the photo, the chat address of a saved contact and the ID, is kept.
	/// </summary>
	public partial class EditContactViewModel : BaseViewModel
	{
		private readonly Contact? existing;
		private readonly string originalState;
		private bool saved;

		/// <summary>
		/// Creates the view model of the page where a contact is typed in or changed.
		/// </summary>
		/// <param name="Args">The contact to change, or none to add a new one.</param>
		public EditContactViewModel(EditContactNavigationArgs? Args)
		{
			this.existing = Args?.Contact;

			VCard Card = this.existing?.GetCard() ?? new VCard();

			this.fullName = Card.DisplayName ?? string.Empty;

			foreach (VCardTypedValue Phone in Card.Telephones)
				this.Phones.Add(new ContactValueItem(Phone.Value, Phone.Types));

			if (this.Phones.Count == 0)
				this.Phones.Add(new ContactValueItem(string.Empty, ["cell"]));

			foreach (VCardTypedValue Email in Card.Emails)
				this.Emails.Add(new ContactValueItem(Email.Value, Email.Types));

			if (this.Emails.Count == 0)
				this.Emails.Add(new ContactValueItem(string.Empty, []));

			VCardAddress? Address = Card.Addresses.Count > 0 ? Card.Addresses[0] : null;
			this.street = Address?.Street ?? string.Empty;
			this.postalCode = Address?.PostalCode ?? string.Empty;
			this.city = Address?.Locality ?? string.Empty;
			this.country = Address?.Country ?? string.Empty;
			this.organization = Card.Organization.Count > 0 ? Card.Organization[0] : string.Empty;
			this.role = Card.Title ?? string.Empty;
			this.birthday = Card.Birthday ?? string.Empty;
			this.notes = Card.Note ?? string.Empty;
			this.labels = string.Join(", ", Card.Categories);

			this.Phones.CollectionChanged += this.Values_CollectionChanged;
			this.Emails.CollectionChanged += this.Values_CollectionChanged;

			foreach (ContactValueItem Item in this.Phones.Concat(this.Emails))
				Item.PropertyChanged += this.Value_PropertyChanged;

			this.originalState = this.StateOf();
		}

		#region Properties

		/// <summary>
		/// The page's title: a new contact, or editing one.
		/// </summary>
		public string Title => this.existing is null ?
			ServiceRef.Localizer[nameof(AppResources.ContactNew)] :
			ServiceRef.Localizer[nameof(AppResources.ContactEdit)];

		/// <summary>
		/// If a new contact is being added. A new contact needs a chat address; a saved one already has it.
		/// </summary>
		public bool IsNew => this.existing is null;

		/// <summary>
		/// The new contact's chat address.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
		private string chatAddress = string.Empty;

		/// <summary>
		/// The contact's full name.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
		private string fullName;

		/// <summary>
		/// Phone numbers.
		/// </summary>
		public ObservableCollection<ContactValueItem> Phones { get; } = [];

		/// <summary>
		/// Email addresses.
		/// </summary>
		public ObservableCollection<ContactValueItem> Emails { get; } = [];

		/// <summary>
		/// Street address.
		/// </summary>
		[ObservableProperty]
		private string street;

		/// <summary>
		/// Postal code.
		/// </summary>
		[ObservableProperty]
		private string postalCode;

		/// <summary>
		/// City.
		/// </summary>
		[ObservableProperty]
		private string city;

		/// <summary>
		/// Country.
		/// </summary>
		[ObservableProperty]
		private string country;

		/// <summary>
		/// Organization.
		/// </summary>
		[ObservableProperty]
		private string organization;

		/// <summary>
		/// Role or title at the organization.
		/// </summary>
		[ObservableProperty]
		private string role;

		/// <summary>
		/// Birthday, as year-month-day.
		/// </summary>
		[ObservableProperty]
		private string birthday;

		/// <summary>
		/// The user's notes about the contact.
		/// </summary>
		[ObservableProperty]
		private string notes;

		/// <summary>
		/// Labels, separated by commas.
		/// </summary>
		[ObservableProperty]
		private string labels;

		#endregion

		#region Values

		private void Values_CollectionChanged(object? Sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems is not null)
			{
				foreach (ContactValueItem Item in e.NewItems.OfType<ContactValueItem>())
					Item.PropertyChanged += this.Value_PropertyChanged;
			}

			if (e.OldItems is not null)
			{
				foreach (ContactValueItem Item in e.OldItems.OfType<ContactValueItem>())
					Item.PropertyChanged -= this.Value_PropertyChanged;
			}

			this.SaveCommand.NotifyCanExecuteChanged();
		}

		private void Value_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			this.SaveCommand.NotifyCanExecuteChanged();
		}

		/// <summary>
		/// Adds a field for another phone number.
		/// </summary>
		[RelayCommand]
		private void AddPhone()
		{
			this.Phones.Add(new ContactValueItem(string.Empty, ["cell"]));
		}

		/// <summary>
		/// Removes a phone number.
		/// </summary>
		/// <param name="Item">The phone number.</param>
		[RelayCommand]
		private void RemovePhone(ContactValueItem? Item)
		{
			if (Item is not null)
				this.Phones.Remove(Item);
		}

		/// <summary>
		/// Adds a field for another email address.
		/// </summary>
		[RelayCommand]
		private void AddEmail()
		{
			this.Emails.Add(new ContactValueItem(string.Empty, []));
		}

		/// <summary>
		/// Removes an email address.
		/// </summary>
		/// <param name="Item">The email address.</param>
		[RelayCommand]
		private void RemoveEmail(ContactValueItem? Item)
		{
			if (Item is not null)
				this.Emails.Remove(Item);
		}

		#endregion

		#region Saving

		private bool CanSave()
		{
			if (this.IsNew)
				return !string.IsNullOrWhiteSpace(this.ChatAddress);

			return !string.IsNullOrWhiteSpace(this.FullName) ||
				this.Phones.Any(Item => !string.IsNullOrWhiteSpace(Item.Text)) ||
				this.Emails.Any(Item => !string.IsNullOrWhiteSpace(Item.Text));
		}

		/// <summary>
		/// Saves the contact. A new contact gets a request and then opens on its card; a changed one goes back to its card. A
		/// contact typed in by the user is never marked as verified.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanSave))]
		private async Task SaveAsync()
		{
			try
			{
				string? NewChatAddress = null;

				if (this.IsNew)
				{
					NewChatAddress = ContactStore.ParseChatAddress(this.ChatAddress);
					if (NewChatAddress is null)
					{
						await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactChatAddress)],
							ServiceRef.Localizer[nameof(AppResources.ContactChatAddressInvalid)]);
						return;
					}

					Contact? Known = await ContactStore.FindByChatAddressAsync(NewChatAddress);
					if (Known is not null)
					{
						await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactNew)],
							ServiceRef.Localizer[nameof(AppResources.ContactAlreadyAdded), Known.DisplayName]);
						return;
					}
				}

				if (!TryNormalizeBirthday(this.Birthday, out string? BirthdayValue))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactBirthday)],
						ServiceRef.Localizer[nameof(AppResources.ContactBirthdayInvalid)]);
					return;
				}

				Contact Contact = this.existing ?? new Contact();
				VCard Card = this.existing?.GetCard() ?? new VCard();

				if (NewChatAddress is not null)
					Card.AddImpp(ContactStore.ChatLinkOf(NewChatAddress));

				string Name = this.FullName.Trim();
				if (Name.Length > 0)
				{
					Card.FormattedName = Name;
					Card.Name = SplitName(Name);
				}
				else
				{
					Card.FormattedName = null;
					Card.Name = null;
				}

				Card.RemoveAll("TEL");
				foreach (ContactValueItem Phone in this.Phones.Where(Item => !string.IsNullOrWhiteSpace(Item.Text)))
					Card.AddTelephone(Phone.Text.Trim(), [.. Phone.Types]);

				Card.RemoveAll("EMAIL");
				foreach (ContactValueItem Email in this.Emails.Where(Item => !string.IsNullOrWhiteSpace(Item.Text)))
					Card.AddEmail(Email.Text.Trim(), [.. Email.Types]);

				this.SaveAddress(Card);

				List<string> Units = [.. Card.Organization.Skip(1)];
				if (string.IsNullOrWhiteSpace(this.Organization))
					Card.SetOrganization();
				else
					Card.SetOrganization([this.Organization.Trim(), .. Units]);

				Card.Title = string.IsNullOrWhiteSpace(this.Role) ? null : this.Role.Trim();
				Card.Birthday = BirthdayValue;
				Card.Note = string.IsNullOrWhiteSpace(this.Notes) ? null : this.Notes.Trim();
				Card.Categories = [.. this.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Distinct(StringComparer.CurrentCultureIgnoreCase)];

				await ContactStore.SaveAsync(Contact, Card);
				this.saved = true;

				if (NewChatAddress is not null)
				{
					// Adding someone sends them a request. Once they accept, their app sends one back, and both can chat and see
					// when the other is online.
					ServiceRef.XmppService.RequestPresenceSubscription(NewChatAddress, ContactRequests.OwnIdentityXml());
					await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactRequestSent), Contact.DisplayName]);

					// The card replaces this page, so going back from it returns to where the contact was added from.
					await ServiceRef.NavigationService.GoToAsync(nameof(ContactCardPage), new ContactCardNavigationArgs(Contact), BackMethod.Pop2);
				}
				else
					await this.GoBack();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Replaces the first address of the card with the one written. Other addresses on the card are kept.
		/// </summary>
		private void SaveAddress(VCard Card)
		{
			IReadOnlyList<VCardAddress> Addresses = Card.Addresses;
			VCardAddress? Old = Addresses.Count > 0 ? Addresses[0] : null;

			Card.RemoveAll("ADR");

			VCardAddress Written = new(Old?.PostOfficeBox ?? string.Empty, Old?.Extended ?? string.Empty, this.Street.Trim(),
				this.City.Trim(), Old?.Region ?? string.Empty, this.PostalCode.Trim(), this.Country.Trim(), Old?.Types ?? new List<string>() { "home" }, null);

			if (!Written.IsEmpty)
				Card.AddAddress(Written);

			foreach (VCardAddress Other in Addresses.Skip(1))
				Card.AddAddress(Other);
		}

		/// <summary>
		/// Splits a full name into the parts of a card's name: the last word is the family name, and the rest are given names.
		/// </summary>
		private static VCardName SplitName(string Name)
		{
			string[] Words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (Words.Length <= 1)
				return new VCardName(string.Empty, Name, string.Empty, string.Empty, string.Empty);

			return new VCardName(Words[^1], string.Join(' ', Words[..^1]), string.Empty, string.Empty, string.Empty);
		}

		/// <summary>
		/// Reads a birthday written as year-month-day, or as --month-day without a year.
		/// </summary>
		/// <param name="Text">The birthday as written.</param>
		/// <param name="Value">The birthday as a card writes it, or null if none was written.</param>
		/// <returns>If the birthday could be read, or none was written.</returns>
		private static bool TryNormalizeBirthday(string? Text, out string? Value)
		{
			Value = null;

			if (string.IsNullOrWhiteSpace(Text))
				return true;

			string Trimmed = Text.Trim();

			if (DateTime.TryParseExact(Trimmed, ["yyyy-MM-dd", "yyyyMMdd", "yyyy-M-d"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Date))
			{
				Value = Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				return true;
			}

			if (Trimmed.StartsWith("--", StringComparison.Ordinal) &&
				DateTime.TryParseExact("2000-" + Trimmed[2..], ["yyyy-MM-dd", "yyyy-MMdd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out Date))
			{
				Value = "--" + Date.ToString("MM-dd", CultureInfo.InvariantCulture);
				return true;
			}

			return false;
		}

		#endregion

		#region Leaving

		/// <summary>
		/// Goes back, after asking, if something has been changed and not saved.
		/// </summary>
		public override async Task GoBack()
		{
			if (!this.saved && !string.Equals(this.StateOf(), this.originalState, StringComparison.Ordinal) &&
				!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactDiscardQuestion)],
					ServiceRef.Localizer[nameof(AppResources.ContactDiscardMessage)],
					ServiceRef.Localizer[nameof(AppResources.ContactDiscard)],
					ServiceRef.Localizer[nameof(AppResources.ContactKeepEditing)]))
			{
				return;
			}

			await base.GoBack();
		}

		/// <summary>
		/// Everything written on the page, as one text, to tell if anything has changed.
		/// </summary>
		private string StateOf()
		{
			StringBuilder State = new();

			State.Append(this.ChatAddress).Append('\n').Append(this.FullName).Append('\n');

			foreach (ContactValueItem Item in this.Phones)
				State.Append(Item.Text).Append('\t');

			State.Append('\n');

			foreach (ContactValueItem Item in this.Emails)
				State.Append(Item.Text).Append('\t');

			State.Append('\n').Append(this.Street).Append('\n').Append(this.PostalCode).Append('\n').Append(this.City)
				.Append('\n').Append(this.Country).Append('\n').Append(this.Organization).Append('\n').Append(this.Role)
				.Append('\n').Append(this.Birthday).Append('\n').Append(this.Notes).Append('\n').Append(this.Labels);

			return State.ToString();
		}

		#endregion
	}
}
