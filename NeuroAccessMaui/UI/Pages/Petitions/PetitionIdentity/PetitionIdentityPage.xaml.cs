using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Image;

namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity
{
	/// <summary>
	/// A page to display when the user is asked to petition an identity.
	/// </summary>
	public partial class PetitionIdentityPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PetitionIdentityPage"/> class.
		/// </summary>
		public PetitionIdentityPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new PetitionIdentityViewModel(ServiceRef.UiService.PopLatestArgs<PetitionIdentityNavigationArgs>());
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			PetitionIdentityViewModel ViewModel = this.ViewModel<PetitionIdentityViewModel>();

			Attachment[]? Attachments = ViewModel.RequestorIdentity?.Attachments;
			if (Attachments is null)
				return;

			ImagesPopup imagesPopup = new();
			ImagesViewModel imagesViewModel = new();
			ServiceRef.UiService.PushAsync(imagesPopup, imagesViewModel);
			imagesViewModel.LoadPhotos(Attachments);
		}
	}
}
