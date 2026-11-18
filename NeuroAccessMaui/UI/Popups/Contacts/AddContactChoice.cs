namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// How the user chose to add a contact.
	/// </summary>
	public enum AddContactChoice
	{
		/// <summary>Scan the other person's code.</summary>
		ScanCode,

		/// <summary>Show the user's own code, for the other person to scan.</summary>
		ShowMyCode,

		/// <summary>Import people from a contact file.</summary>
		ImportFile,

		/// <summary>Type in a name, phone and email.</summary>
		TypeIn
	}
}
