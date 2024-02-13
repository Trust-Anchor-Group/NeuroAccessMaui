using Waher.Networking.XMPP.Contracts;

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
		public PetitionSignaturePage(PetitionSignatureViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			PetitionSignatureViewModel ViewModel = this.ViewModel<PetitionSignatureViewModel>();

			Attachment[]? Attachments = ViewModel.RequestorIdentity?.Attachments;
			if (Attachments is null)
				this.PhotoViewer.HidePhotos();
			else
				this.PhotoViewer.ShowPhotos(Attachments);
		}
	}
}
