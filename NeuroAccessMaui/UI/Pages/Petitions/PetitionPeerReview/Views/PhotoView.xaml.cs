namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class PhotoView
	{
		public static PhotoView Create()
		{
			return Create<PhotoView>();
		}

		public PhotoView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
