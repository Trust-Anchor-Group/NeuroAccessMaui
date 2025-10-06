using NeuroAccessMaui.Test;

namespace NeuroAccessMaui.Services.UI.Popups
{
	/// <summary>
	/// Options controlling popup presentation and behavior.
	/// </summary>
	public class PopupOptions
	{
		/// <summary>
		/// Gets or sets the transition used when the popup is shown.
		/// </summary>
		public PopupTransition ShowTransition { get; set; } = PopupTransition.Scale;

		/// <summary>
		/// Gets or sets the transition used when the popup is dismissed.
		/// </summary>
		public PopupTransition HideTransition { get; set; } = PopupTransition.Fade;

		/// <summary>
		/// Gets or sets if tapping the dimmed background should close the popup.
		/// </summary>
		public bool CloseOnBackgroundTap { get; set; } = true;

		/// <summary>
		/// Gets or sets if the hardware back button should close the popup.
		/// </summary>
		public bool CloseOnBackButton { get; set; } = true;

		/// <summary>
		/// Gets or sets the opacity applied to the backdrop overlay when the popup is visible.
		/// Value is clamped between 0 and 1.
		/// </summary>
		public double OverlayOpacity
		{
			get => this.overlayOpacity;
			set
			{
				double Clamped = value;
				if (Clamped < 0)
					Clamped = 0;
				else if (Clamped > 1)
					Clamped = 1;
				this.overlayOpacity = Clamped;
			}
		}

		private double overlayOpacity = 0.7;
	}
}
