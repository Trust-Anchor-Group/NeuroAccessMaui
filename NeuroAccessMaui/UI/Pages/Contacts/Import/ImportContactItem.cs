using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;

namespace NeuroAccessMaui.UI.Pages.Contacts.Import
{
	/// <summary>
	/// A card in a contact file, as a row on the import page, which the user can choose to import or not.
	/// </summary>
	public sealed partial class ImportContactItem : ObservableObject
	{
		private readonly Action selectionChanged;

		/// <summary>
		/// A card in a contact file, as a row on the import page.
		/// </summary>
		/// <param name="Item">The card, with what importing it does.</param>
		/// <param name="SelectionChanged">Called when the row is chosen or left out.</param>
		public ImportContactItem(ContactImportItem Item, Action SelectionChanged)
		{
			this.Item = Item;
			this.selectionChanged = SelectionChanged;
			this.Name = Item.Name;
			this.Initials = GetInitials(Item.Name);
			this.StatusText = GetStatus(Item);
			this.isSelected = this.CanSelect;
		}

		/// <summary>
		/// The card, with what importing it does.
		/// </summary>
		public ContactImportItem Item { get; }

		/// <summary>
		/// The name on the card.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Initials shown on the avatar.
		/// </summary>
		public string Initials { get; }

		/// <summary>
		/// What importing the card does: a new contact, the changes to a contact, or nothing.
		/// </summary>
		public string StatusText { get; }

		/// <summary>
		/// If importing the card changes a contact the user has.
		/// </summary>
		public bool IsUpdate => this.Item.Kind == ContactImportKind.Update;

		/// <summary>
		/// If the card can be imported. A card with nothing new can't.
		/// </summary>
		public bool CanSelect => this.Item.Kind != ContactImportKind.Unchanged;

		/// <summary>
		/// If the card is to be imported.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AccessibleText))]
		private bool isSelected;

		/// <summary>
		/// The row, as read by a screen reader: the name, what importing it does, and if it is chosen.
		/// </summary>
		public string AccessibleText => this.Name + ", " + this.StatusText + ", " +
			(this.IsSelected ? ServiceRef.Localizer[nameof(AppResources.ContactImportSelected)] : ServiceRef.Localizer[nameof(AppResources.ContactImportNotSelected)]);

		/// <summary>
		/// Chooses the card, or leaves it out.
		/// </summary>
		[RelayCommand]
		private void Toggle()
		{
			if (!this.CanSelect)
				return;

			this.IsSelected = !this.IsSelected;
		}

		partial void OnIsSelectedChanged(bool value)
		{
			this.selectionChanged();
		}

		private static string GetStatus(ContactImportItem Item)
		{
			switch (Item.Kind)
			{
				case ContactImportKind.New:
					List<string> Details = [];

					if (Item.Card.Telephones.Count > 0)
						Details.Add(ServiceRef.Localizer[nameof(AppResources.ContactImportFieldPhone)]);

					if (Item.Card.Emails.Count > 0)
						Details.Add(ServiceRef.Localizer[nameof(AppResources.ContactImportFieldEmail)]);

					if (Details.Count == 0)
						return ServiceRef.Localizer[nameof(AppResources.ContactImportNew)];

					return ServiceRef.Localizer[nameof(AppResources.ContactImportNewWith), string.Join(", ", Details)];

				case ContactImportKind.Update:
					return ServiceRef.Localizer[nameof(AppResources.ContactImportUpdates), DescribeChanges(Item.Changes)];

				default:
					return ServiceRef.Localizer[nameof(AppResources.ContactImportUnchanged)];
			}
		}

		/// <summary>
		/// The parts of a contact that importing a card changes, as a short list in words.
		/// </summary>
		private static string DescribeChanges(ContactFields Changes)
		{
			List<string> Parts = [];

			Add(Parts, Changes, ContactFields.Name, nameof(AppResources.ContactImportFieldName));
			Add(Parts, Changes, ContactFields.Phone, nameof(AppResources.ContactImportFieldPhone));
			Add(Parts, Changes, ContactFields.Email, nameof(AppResources.ContactImportFieldEmail));
			Add(Parts, Changes, ContactFields.Address, nameof(AppResources.ContactImportFieldAddress));
			Add(Parts, Changes, ContactFields.Organization, nameof(AppResources.ContactImportFieldOrganization));
			Add(Parts, Changes, ContactFields.Photo, nameof(AppResources.ContactImportFieldPhoto));
			Add(Parts, Changes, ContactFields.Notes, nameof(AppResources.ContactImportFieldNotes));
			Add(Parts, Changes, ContactFields.Labels, nameof(AppResources.ContactImportFieldLabels));
			Add(Parts, Changes, ContactFields.Birthday, nameof(AppResources.ContactImportFieldBirthday));
			Add(Parts, Changes, ContactFields.ChatAddress, nameof(AppResources.ContactImportFieldChatAddress));

			return string.Join(", ", Parts);
		}

		private static void Add(List<string> Parts, ContactFields Changes, ContactFields Field, string Key)
		{
			if ((Changes & Field) != 0)
				Parts.Add(ServiceRef.Localizer[Key]);
		}

		/// <summary>
		/// The initials of a name: the first letters of its first two words.
		/// </summary>
		private static string GetInitials(string Name)
		{
			string[] Words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			string Result = string.Concat(Words.Take(2).Select(Word => char.ToUpper(Word[0], CultureInfo.CurrentCulture)));
			return Result.Length == 0 ? "?" : Result;
		}
	}
}
