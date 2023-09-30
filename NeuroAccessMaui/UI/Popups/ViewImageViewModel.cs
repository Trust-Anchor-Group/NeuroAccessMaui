using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.UI.Photos;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Popups;

/// <summary>
///  The class to use as binding context for displaying images.
/// </summary>
public partial class ViewImageViewModel : ObservableObject
{
	private readonly PhotosLoader photosLoader;

	/// <summary>
	/// Creates a new instance of the <see cref="ViewImageModel"/> class.
	/// </summary>
	public ViewImageViewModel()
	{
		this.photosLoader = new PhotosLoader(this.Photos);
	}

	/// <summary>
	/// Holds the list of photos to display.
	/// </summary>
	public ObservableCollection<Photo> Photos { get; } = new();

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
