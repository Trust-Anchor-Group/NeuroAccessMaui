using System.ComponentModel;
using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Image;
namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionContract
{
	/// <summary>
	/// A page to display when the user is asked to petition a contract.
	/// </summary>
	[DesignTimeVisible(true)]
	public partial class PetitionContractPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PetitionContractPage"/> class.
		/// </summary>
		public PetitionContractPage()
		{
			this.ContentPageModel = new PetitionContractViewModel(ServiceRef.NavigationService.PopLatestArgs<PetitionContractNavigationArgs>());
			this.InitializeComponent();
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			if (this.ContentPageModel is PetitionContractViewModel PetitionContractViewModel)
			{
				Attachment[]? Attachments = PetitionContractViewModel.RequestorIdentity?.Attachments;
				if (Attachments is null)
					return;

				ImagesPopup ImagesPopup = new();
				ImagesViewModel ImagesViewModel = new(Attachments);
				ImagesPopup.BindingContext = ImagesViewModel;
				ServiceRef.PopupService.PushAsync(ImagesPopup);
				//imagesViewModel.LoadPhotos(Attachments);
			}
		}
	}
}
