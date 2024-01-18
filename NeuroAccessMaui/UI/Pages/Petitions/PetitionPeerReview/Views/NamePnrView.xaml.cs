namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class NamePnrView
	{
		public static NamePnrView Create()
		{
			return Create<NamePnrView>();
		}

		public NamePnrView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
