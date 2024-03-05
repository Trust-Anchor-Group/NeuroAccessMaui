using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Image;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// A page that displays a specific contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewContractPage"/> class.
		/// </summary>
		public ViewContractPage()
		{
			this.ContentPageModel = new ViewContractViewModel(ServiceRef.UiService.PopLatestArgs<ViewContractNavigationArgs>());
			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			ViewContractViewModel ViewModel = this.ViewModel<ViewContractViewModel>();

			Attachment[]? Attachments = ViewModel.Contract?.Attachments;
			if (Attachments is null)
				return;

			ImagesPopup ImagesPopup = new();
			ImagesViewModel ImagesViewModel = new();
			ServiceRef.UiService.PushAsync(ImagesPopup, ImagesViewModel);
			ImagesViewModel.LoadPhotos(Attachments);
		}
	}
}
