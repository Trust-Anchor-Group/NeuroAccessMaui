using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.Pages;
using System.Collections.ObjectModel;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Popups.Photos.Image
{
	/// <summary>
	///  The class to use as binding context for displaying images.
	/// </summary>
	public partial class ImageViewModel : BaseViewModel
	{
		private readonly PhotosLoader photosLoader;

		/// <summary>
		/// Creates a new instance of the <see cref="ImageViewModel"/> class.
		/// </summary>
		public ImageViewModel()
		{
			this.Photos = [];
			this.photosLoader = new PhotosLoader(this.Photos);
		}

		/// <summary>
		/// Holds the list of photos to display.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; }

		/// <summary>
		/// Gets or sets whether a user can swipe to see the photos.
		/// </summary>
		[ObservableProperty]
		private bool isSwipeEnabled;

		/// <summary>
		/// Loads the attachments photos, if there are any.
		/// </summary>
		/// <param name="attachments">The attachments to load.</param>
		public void LoadPhotos(Attachment[] attachments)
		{
			this.photosLoader.CancelLoadPhotos();
			this.IsSwipeEnabled = false;

			_ = this.photosLoader.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys, () =>
			{
				MainThread.BeginInvokeOnMainThread(() => this.IsSwipeEnabled = this.Photos.Count > 1);
			});
		}

		/// <summary>
		/// Clears the currently displayed photos.
		/// </summary>
		public void ClearPhotos()
		{
			this.photosLoader.CancelLoadPhotos();
		}
	}
}
