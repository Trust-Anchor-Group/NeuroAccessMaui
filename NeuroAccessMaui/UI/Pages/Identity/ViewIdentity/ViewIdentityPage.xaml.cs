using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Image;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Identity.ViewIdentity
{
	/// <summary>
	/// A page to display when the user wants to view an identity.
	/// </summary>
	public partial class ViewIdentityPage
	{
		private const double MaxTiltAngle = .4;

		/// <summary>
		/// Creates a new instance of the <see cref="ViewIdentityPage"/> class.
		/// </summary>
		public ViewIdentityPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new ViewIdentityViewModel(ServiceRef.UiService.PopLatestArgs<ViewIdentityNavigationArgs>());
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			//!!! this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			ViewIdentityViewModel ViewModel = this.ViewModel<ViewIdentityViewModel>();

			Attachment[]? Attachments = ViewModel.LegalIdentity?.Attachments;
			if (Attachments is null)
				return;

			ImagesPopup ImagesPopup = new();
			ImagesViewModel ImagesViewModel = new(Attachments);
			ServiceRef.UiService.PushAsync(ImagesPopup, ImagesViewModel);
			//imagesViewModel.LoadPhotos(Attachments);
		}

		// Handle the start of the touch interaction.
		private void GestureOverlay_StartInteraction(object sender, TouchEventArgs e)
		{
			if (e.Touches is not null && e.Touches.Length > 0)
				UpdateTilt(ToPoint(e.Touches[0]));
		}

		// Handle touch drag events.
		private void GestureOverlay_DragInteraction(object sender, TouchEventArgs e)
		{
			if (e.Touches is not null && e.Touches.Length > 0)
				UpdateTilt(ToPoint(e.Touches[0]));
		}

		// Handle the end of the interaction.
		private void GestureOverlay_EndInteraction(object sender, TouchEventArgs e)
		{
			ResetTilt();
		}

		// Handle cancelled interactions.
		private void GestureOverlay_CancelInteraction(object sender, EventArgs e)
		{
			ResetTilt();
		}

		/// <summary>
		/// Computes and applies the tilt effect based on the pointer position.
		/// </summary>
		/// <param name="pointerPosition">Pointer position relative to the Border.</param>
		private void UpdateTilt(Point pointerPosition)
		{
			// Ensure the TiltBorder has valid dimensions.
			if (TiltBorder.Width <= 0 || TiltBorder.Height <= 0)
				return;

			// Compute the center of the Border.
			double centerX = TiltBorder.Width / 2;
			double centerY = TiltBorder.Height / 2;

			// Calculate how far the pointer is from the center.
			double deltaX = pointerPosition.X - centerX;
			double deltaY = pointerPosition.Y - centerY;

			// Normalize the differences so that they roughly range from -1 to 1.
			double normalizedX = deltaX / centerX;
			double normalizedY = deltaY / centerY;

			// Compute tilt rotations:
			// - RotationY (yaw) is determined by the horizontal offset.
			// - RotationX (pitch) is determined by the vertical offset (inverted for natural effect).
			double rotationY = normalizedX * MaxTiltAngle;
			double rotationX = -normalizedY * MaxTiltAngle;

			// Apply the computed rotations to the Border.
			TiltBorder.RotationY = rotationY;
			TiltBorder.RotationX = rotationX;
		}

		/// <summary>
		/// Resets the Border’s rotations to their default (zero) values.
		/// </summary>
		private void ResetTilt()
		{
			TiltBorder.RotationX = 0;
			TiltBorder.RotationY = 0;
		}

		/// <summary>
		/// Converts a Microsoft.Maui.Graphics.PointF (from TouchEventArgs) to Microsoft.Maui.Controls.Point.
		/// </summary>
		private Point ToPoint(PointF pointF)
		{
			return new Point(pointF.X, pointF.Y);
		}

	}
}
