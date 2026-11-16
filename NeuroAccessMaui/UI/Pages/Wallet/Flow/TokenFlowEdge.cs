namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// An arrow on the flow map, from one step's card to another's.
	/// </summary>
	/// <param name="From">Where the card of the step the arrow starts at is.</param>
	/// <param name="To">Where the card of the step the arrow points to is.</param>
	/// <param name="IsLoop">If the arrow goes back to the step it starts at.</param>
	/// <param name="IsForward">If the arrow points to a step further down the map.</param>
	/// <param name="IsFailure">If the token only goes this way when something fails. The arrow is dashed.</param>
	/// <param name="IsFromCurrent">If the arrow starts at the step the token is in now. The arrow is in the accent colour.</param>
	/// <param name="Label">What moves the token this way, in plain words.</param>
	public sealed record TokenFlowEdge(Rect From, Rect To, bool IsLoop, bool IsForward, bool IsFailure, bool IsFromCurrent, string Label);
}
