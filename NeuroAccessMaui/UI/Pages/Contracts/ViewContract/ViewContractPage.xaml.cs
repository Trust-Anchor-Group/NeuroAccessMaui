using CommunityToolkit.Maui.Layouts;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
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
		public ViewContractPage(ViewContractViewModel ViewModel)
		{
			this.InitializeComponent();

			StateContainer.SetCurrentState(this.StateGrid, ViewContractStep.Loading.ToString());
			ViewModel.StateObject = this.StateGrid;
			this.BindingContext = ViewModel;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			ViewContractViewModel ViewModel = this.ViewModel<ViewContractViewModel>();

			Attachment[]? Attachments = ViewModel.Contract?.Contract.Attachments;
			if (Attachments is null)
				return;

			ImagesPopup ImagesPopup = new();
			ImagesViewModel ImagesViewModel = new(Attachments);
			ServiceRef.UiService.PushAsync(ImagesPopup, ImagesViewModel);
			//imagesViewModel.LoadPhotos(Attachments);
		}
	}
}
