using NeuroAccessMaui.Extensions;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// A generic UI component to display images.
	/// </summary>
	public partial class ViewImagePopup
	{
		private readonly ViewImageViewModel viewModel;
		private const uint durationInMs = 300;

		/// <summary>
		/// Creates a new instance of the <see cref="ViewImagePopup"/> class.
		/// </summary>
		public ViewImagePopup(ViewImageViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;
			this.viewModel = ViewModel;
		}

		/// <summary>
		/// Shows the attachments photos in the current view.
		/// </summary>
		/// <param name="Attachments">The attachments to show.</param>
		public void ShowPhotos(Attachment[] Attachments)
		{
			if (Attachments is null || Attachments.Length <= 0)
				return;

			Attachment[] ImageAttachments = Attachments.GetImageAttachments().ToArray();

			if (ImageAttachments.Length <= 0)
				return;

			this.IsVisible = true;
			this.viewModel.LoadPhotos(Attachments);

			MainThread.BeginInvokeOnMainThread(/*async*/ () =>
			{
				//!!! await this.PhotoViewer.FadeTo(1d, durationInMs, Easing.SinIn);
			});
		}

		/// <summary>
		/// Hides the photos from view.
		/// </summary>
		public void HidePhotos()
		{
			//!!! this.PhotoViewer.Opacity = 0;
			this.viewModel.ClearPhotos();
			this.IsVisible = false;
		}

		private void CloseIcon_Tapped(object? Sender, EventArgs e)
		{
			this.HidePhotos();
		}

		/// <summary>
		/// Gets if photos are showing or not.
		/// </summary>
		/// <returns>If photos are showing</returns>
		public bool PhotosAreShowing()
		{
			//!!! return this.PhotoViewer.Opacity > 0 && this.IsVisible;
			return this.IsVisible;
		}

		private void PopupPage_BackgroundClicked(object sender, EventArgs e)
		{

		}
	}
}
