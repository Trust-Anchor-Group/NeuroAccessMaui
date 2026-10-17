namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Where a row sits in a list that is drawn as one card.
	/// </summary>
	public enum ListCardPosition
	{
		/// <summary>
		/// The only row. The card is rounded at the top and at the bottom.
		/// </summary>
		Only,

		/// <summary>
		/// The first of several rows. The card is rounded at the top.
		/// </summary>
		First,

		/// <summary>
		/// A row between the first and the last. It has a divider above it.
		/// </summary>
		Middle,

		/// <summary>
		/// The last of several rows. The card is rounded at the bottom, and the row has a divider above it.
		/// </summary>
		Last
	}
}
