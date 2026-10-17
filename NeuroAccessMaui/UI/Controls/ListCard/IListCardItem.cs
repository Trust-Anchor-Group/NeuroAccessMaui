namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// An item in a list that is drawn as one card. The list tells the item where it sits, so its row can draw its own
	/// part of the card.
	/// </summary>
	public interface IListCardItem
	{
		/// <summary>
		/// Where the item sits in the list. Changes are announced, so the row redraws.
		/// </summary>
		ListCardPosition CardPosition { get; set; }
	}
}
