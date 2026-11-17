using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;

namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// An arrow on the flow map, from one step's card to another's.
	/// </summary>
	/// <param name="From">Where the card of the step the arrow starts at is.</param>
	/// <param name="To">Where the card of the step the arrow points to is.</param>
	/// <param name="Route">How the arrow runs from one card to the other.</param>
	/// <param name="OnLeft">If a loop or a lane runs on the left side of the cards, else on the right.</param>
	/// <param name="LaneX">Where a lane runs across the map, beside every card of the rows it passes.</param>
	/// <param name="Shift">How far above (negative) or below the middle of the cards an arrow beside them runs, so arrows
	/// both ways between the same cards lie apart.</param>
	/// <param name="IsFailure">If the token only goes this way when something fails. The arrow is dashed.</param>
	/// <param name="IsFromCurrent">If the arrow starts at the step the token is in now. The arrow is in the accent colour.</param>
	/// <param name="Label">What moves the token this way, as whole sentences. Screen readers read it.</param>
	/// <param name="ShortLabel">What moves the token this way in a few words, shown on the arrow.</param>
	/// <param name="Icon">The icon of the trigger, shown before <paramref name="ShortLabel"/>.</param>
	/// <param name="FromStep">The step the arrow starts at.</param>
	/// <param name="Exit">The way out of <paramref name="FromStep"/> the arrow shows. When several lead to the same step, the
	/// first of them.</param>
	public sealed record TokenFlowEdge(Rect From, Rect To, TokenFlowRoute Route, bool OnLeft, double LaneX, double Shift, bool IsFailure,
		bool IsFromCurrent, string Label, string ShortLabel, Geometry Icon, TokenFlowStepItem FromStep, TokenFlowExit Exit);
}
