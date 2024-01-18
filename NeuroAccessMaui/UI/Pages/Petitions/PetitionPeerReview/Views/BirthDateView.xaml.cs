namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class BirthDateView
	{
		public static BirthDateView Create()
		{
			return Create<BirthDateView>();
		}

		public BirthDateView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
