using NeuroAccessMaui.Extensions;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Popups.Photos.Image
{
	/// <summary>
	/// A generic UI component to display an image.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ImageView
	{
		private const uint durationInMs = 300;

		/// <summary>
		/// Creates a new instance of the <see cref="ImageView"/> class.
		/// </summary>
		public ImageView()
		{
			this.InitializeComponent();
			this.ContentPageModel = new ImageViewModel();
		}

		/// <summary>
		/// Shows the attachments photos in the current view.
		/// </summary>
		/// <param name="attachments">The attachments to show.</param>
		public void ShowPhotos(Attachment[] attachments)
		{
			if (attachments is null || attachments.Length <= 0)
				return;

			Attachment[] imageAttachments = attachments.GetImageAttachments().ToArray();
			if (imageAttachments.Length <= 0)
				return;

			this.IsVisible = true;

			this.ViewModel<ImageViewModel>().LoadPhotos(attachments);
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await this.PhotoViewer.FadeTo(1d, durationInMs, Easing.SinIn);
			});
		}

		/// <summary>
		/// Hides the photos from view.
		/// </summary>
		public void HidePhotos()
		{
			this.PhotoViewer.Opacity = 0;
			this.ViewModel<ImageViewModel>().ClearPhotos();
			this.IsVisible = false;
		}

		private void CloseIcon_Tapped(object Sender, EventArgs e)
		{
			this.HidePhotos();
		}

		/// <summary>
		/// Gets if photos are showing or not.
		/// </summary>
		/// <returns>If photos are showing</returns>
		public bool PhotosAreShowing()
		{
			return this.PhotoViewer.Opacity > 0 && this.IsVisible;
		}
	}
}
