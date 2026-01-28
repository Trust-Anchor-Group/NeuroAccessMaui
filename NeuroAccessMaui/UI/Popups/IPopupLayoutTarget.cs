using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Defines layout settings that can be applied to a popup by the popup service.
	/// </summary>
	public interface IPopupLayoutTarget
	{
		/// <summary>
		/// Gets or sets whether layout-related popup options should be applied.
		/// </summary>
		bool AllowLayoutOverrides { get; set; }

		/// <summary>
		/// Gets or sets the placement strategy for the popup content.
		/// </summary>
		PopupPlacement Placement { get; set; }

		/// <summary>
		/// Gets or sets the anchor point used when <see cref="PopupPlacement.Anchor"/> is selected.
		/// </summary>
		Point? AnchorPoint { get; set; }

		/// <summary>
		/// Gets or sets the margin reserved around the popup content.
		/// </summary>
		Thickness PopupMargin { get; set; }

		/// <summary>
		/// Gets or sets the padding applied inside the popup container.
		/// </summary>
		Thickness PopupPadding { get; set; }

		/// <summary>
		/// Gets or sets whether taps outside the popup content should close the popup.
		/// </summary>
		bool CloseOnBackgroundTap { get; set; }
	}
}
