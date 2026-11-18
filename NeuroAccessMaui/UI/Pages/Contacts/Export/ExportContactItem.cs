using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.Export
{
	/// <summary>
	/// A contact, as a row on the export page, which the user can choose to export or not.
	/// </summary>
	public sealed partial class ExportContactItem : ObservableObject
	{
		private readonly Action selectionChanged;

		/// <summary>
		/// A contact, as a row on the export page. Blocked contacts start out not chosen.
		/// </summary>
		/// <param name="Contact">The contact.</param>
		/// <param name="SelectionChanged">Called when the row is chosen or left out.</param>
		public ExportContactItem(Contact Contact, Action SelectionChanged)
		{
			this.Contact = Contact;
			this.selectionChanged = SelectionChanged;
			this.Name = Contact.DisplayName;
			this.Initials = GetInitials(Contact.DisplayName);
			this.isSelected = !Contact.IsBlocked;
		}

		/// <summary>
		/// The contact.
		/// </summary>
		public Contact Contact { get; }

		/// <summary>
		/// The contact's name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Initials shown on the avatar.
		/// </summary>
		public string Initials { get; }

		/// <summary>
		/// If the user has blocked the contact.
		/// </summary>
		public bool IsBlocked => this.Contact.IsBlocked;

		/// <summary>
		/// If the contact is to be exported.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AccessibleText))]
		private bool isSelected;

		/// <summary>
		/// The row, as read by a screen reader: the name, if the contact is blocked, and if it is chosen.
		/// </summary>
		public string AccessibleText
		{
			get
			{
				string Text = this.Name;

				if (this.IsBlocked)
					Text += ", " + ServiceRef.Localizer[nameof(AppResources.ContactExportBlocked)];

				return Text + ", " + (this.IsSelected ?
					ServiceRef.Localizer[nameof(AppResources.ContactImportSelected)] :
					ServiceRef.Localizer[nameof(AppResources.ContactImportNotSelected)]);
			}
		}

		/// <summary>
		/// Chooses the contact, or leaves it out.
		/// </summary>
		[RelayCommand]
		private void Toggle()
		{
			this.IsSelected = !this.IsSelected;
		}

		partial void OnIsSelectedChanged(bool value)
		{
			this.selectionChanged();
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
