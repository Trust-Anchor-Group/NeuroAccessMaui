namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// A card read from a contact file, with what importing it would do.
	/// </summary>
	/// <param name="Card">The card, as read from the file.</param>
	/// <param name="Existing">The contact the card matches, if any.</param>
	/// <param name="Kind">What importing the card does.</param>
	/// <param name="Changes">What importing the card adds to or changes in the existing contact.</param>
	public sealed record ContactImportItem(VCard.VCard Card, Contact? Existing, ContactImportKind Kind, ContactFields Changes)
	{
		/// <summary>
		/// The name to show for the card.
		/// </summary>
		public string Name => ContactStore.NameOf(this.Card);
	}
}
