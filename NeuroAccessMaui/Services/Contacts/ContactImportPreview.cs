namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// What importing a contact file would do: the cards that can be imported, and how many cards were skipped because they
	/// have no chat address.
	/// </summary>
	/// <param name="Items">The cards that can be imported, with what importing each one does.</param>
	/// <param name="SkippedCount">The number of cards skipped because they have no chat address.</param>
	public sealed record ContactImportPreview(IReadOnlyList<ContactImportItem> Items, int SkippedCount);
}
