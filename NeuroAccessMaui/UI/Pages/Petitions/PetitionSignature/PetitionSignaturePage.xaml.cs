using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Image;
namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature
{
	/// <summary>
	/// A page to display when the user is asked to petition a signature.
	/// </summary>
	public partial class PetitionSignaturePage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PetitionSignaturePage"/> class.
		/// </summary>
		public PetitionSignaturePage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new PetitionSignatureViewModel(ServiceRef.UiService.PopLatestArgs<PetitionSignatureNavigationArgs>());
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			PetitionSignatureViewModel ViewModel = this.ViewModel<PetitionSignatureViewModel>();

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
