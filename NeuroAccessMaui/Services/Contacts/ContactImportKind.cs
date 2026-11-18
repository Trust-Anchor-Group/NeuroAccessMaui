namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// What importing a card from a contact file does.
	/// </summary>
	public enum ContactImportKind
	{
		/// <summary>The card is a new contact.</summary>
		New,

		/// <summary>The card adds to or changes a contact the user has.</summary>
		Update,

		/// <summary>The user has the contact already, and the card has nothing new.</summary>
		Unchanged
	}
}
