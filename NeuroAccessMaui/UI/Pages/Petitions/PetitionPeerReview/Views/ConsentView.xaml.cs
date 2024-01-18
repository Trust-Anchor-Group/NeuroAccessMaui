namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class ConsentView
	{
		public static ConsentView Create()
		{
			return Create<ConsentView>();
		}

		public ConsentView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
