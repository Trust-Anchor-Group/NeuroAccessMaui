using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.Services.UI.Popups
{
	/// <summary>
	/// Options controlling popup presentation and behavior.
	/// </summary>
	public class PopupOptions
	{
		private double overlayOpacity = 0.7;

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
		/// Ignored if <see cref="IsBlocking"/> or <see cref="DisableBackgroundTap"/> is true.
		/// </summary>
		public bool CloseOnBackgroundTap { get; set; } = true;

		/// <summary>
		/// Gets or sets if the hardware back button should close the popup.
		/// For blocking popups this is typically set to true to allow dismissal.
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
				double clamped = value;
				if (clamped < 0)
					clamped = 0;
				else if (clamped > 1)
					clamped = 1;
				this.overlayOpacity = clamped;
			}
		}

		/// <summary>
		/// If true, popup behaves as a blocking modal (single logical layer). Background taps are disabled unless explicitly overridden.
		/// </summary>
		public bool IsBlocking { get; set; } = false;

		/// <summary>
		/// Explicit flag to disable background tap regardless of <see cref="CloseOnBackgroundTap"/>.
		/// </summary>
		public bool DisableBackgroundTap { get; set; } = false;

		/// <summary>
		/// Defines how the popup content should be positioned on screen.
		/// </summary>
		public PopupPlacement Placement { get; set; } = PopupPlacement.Center;

		/// <summary>
		/// Optional anchor coordinate in device-independent units used with <see cref="PopupPlacement.Anchor"/>.
		/// </summary>
		public Point? AnchorPoint { get; set; }

		/// <summary>
		/// Margin applied around the popup container.
		/// </summary>
		public Thickness Margin { get; set; } = new Thickness(0);

		/// <summary>
		/// Padding applied inside the popup container before measuring child content.
		/// </summary>
		public Thickness Padding { get; set; } = new Thickness(0);

		/// <summary>
		/// Normalizes option combinations enforcing blocking semantics.
		/// </summary>
		public void Normalize()
		{
			if (this.IsBlocking)
			{
				// Blocking popups should not close on background tap unless developer explicitly allows it.
				if (this.CloseOnBackgroundTap && !this.DisableBackgroundTap)
					this.CloseOnBackgroundTap = false;
				// Ensure overlay is at least semi-opaque for modals.
				if (this.OverlayOpacity < 0.3)
					this.OverlayOpacity = 0.7;
			}
			if (this.DisableBackgroundTap)
				this.CloseOnBackgroundTap = false;
		}

		/// <summary>
		/// Creates standard blocking modal popup options.
		/// </summary>
		/// <param name="overlayOpacity">Optional overlay opacity (default 0.7).</param>
		/// <param name="closeOnBackButton">If back button dismisses (default true).</param>
		/// <returns>Configured blocking <see cref="PopupOptions"/>.</returns>
		public static PopupOptions CreateModal(double overlayOpacity = 0.7, bool closeOnBackButton = true)
		{
			PopupOptions options = new()
			{
				IsBlocking = true,
				DisableBackgroundTap = true,
				OverlayOpacity = overlayOpacity,
				CloseOnBackButton = closeOnBackButton,
				ShowTransition = PopupTransition.Scale,
				HideTransition = PopupTransition.Fade
			};
			options.Normalize();
			return options;
		}
	}
}
