namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// What the user chose in the menu of the contacts list.
	/// </summary>
	public enum ContactsMenuChoice
	{
		/// <summary>Import people from a contact file.</summary>
		Import,

		/// <summary>Share contacts as a contact file.</summary>
		Export,

		/// <summary>Show the people the user has blocked.</summary>
		Blocked,

		/// <summary>Back up the contacts, or restore them.</summary>
		Backup
	}
}
