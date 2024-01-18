namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class ApprovedView
	{
		public static ApprovedView Create()
		{
			return Create<ApprovedView>();
		}

		public ApprovedView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
