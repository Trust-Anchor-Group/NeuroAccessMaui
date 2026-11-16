using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// One way the token moves on from a step, as a line with an icon, such as a calendar and "On 1 Jun 2026".
	/// </summary>
	/// <param name="Icon">The icon that shows what kind of trigger it is.</param>
	/// <param name="Text">What moves the token on, in plain words.</param>
	public sealed record TokenFlowWhenLine(Geometry Icon, string Text);
}
