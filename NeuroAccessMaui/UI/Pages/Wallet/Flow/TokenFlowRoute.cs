namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// How an arrow on the flow map runs from one step's card to another's. Each way keeps the arrow out from behind other
	/// cards, so it can't look as if it starts or ends at a card it only passes.
	/// </summary>
	public enum TokenFlowRoute
	{
		/// <summary>
		/// Down to a card in the next row: from the bottom of one card to the top of the other.
		/// </summary>
		Down,

		/// <summary>
		/// Back to the card it starts at, on the side of the card that has no neighbour.
		/// </summary>
		Loop,

		/// <summary>
		/// Back up the map in a lane beside the cards, on a side where neither card has a neighbour.
		/// </summary>
		Lane,

		/// <summary>
		/// To the card next to it in the same row, straight across the space between them. That space is widened, so the
		/// label fits in it.
		/// </summary>
		Beside,

		/// <summary>
		/// To another card further along the same row, in a curve under the cards between them.
		/// </summary>
		UnderRow,

		/// <summary>
		/// Back up the map from the top of one card to the bottom of the other, when both sides are taken.
		/// </summary>
		Up
	}
}
