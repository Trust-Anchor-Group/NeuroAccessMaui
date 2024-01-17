namespace NeuroAccessMaui.UI.Pages.Petitions
{
	/// <summary>
	/// A page to display when the user is asked to review an identity application.
	/// </summary>
	public partial class PetitionPeerReviewPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PetitionPeerReviewPage"/> class.
		/// </summary>
		public PetitionPeerReviewPage(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			//!!! this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object? Sender, EventArgs e)
		{
			PetitionPeerReviewViewModel ViewModel = this.ViewModel<PetitionPeerReviewViewModel>();

			//!!! Attachment[] Attachments = ViewModel.RequestorIdentity?.Attachments;
			//!!! this.PhotoViewer.ShowPhotos(Attachments);
		}
	}
}
