using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// One way the selected step moves on, shown in the sheet under the flow map.
	/// </summary>
	/// <param name="Icon">The icon that shows what kind of trigger it is.</param>
	/// <param name="Title">What moves the token on, such as "On 1 Jun 2026".</param>
	/// <param name="Subtitle">Where it goes, such as "Goes to Done", or an empty string.</param>
	/// <param name="IsMarked">If the user tapped this way's arrow on the map, so the line is marked.</param>
	public sealed record TokenFlowSheetLine(Geometry Icon, string Title, string Subtitle, bool IsMarked)
	{
		/// <summary>
		/// If there is a line about where the token goes.
		/// </summary>
		public bool HasSubtitle => !string.IsNullOrEmpty(this.Subtitle);
	}
}
