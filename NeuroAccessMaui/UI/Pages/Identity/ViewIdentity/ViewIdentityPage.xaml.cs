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


	}
}
