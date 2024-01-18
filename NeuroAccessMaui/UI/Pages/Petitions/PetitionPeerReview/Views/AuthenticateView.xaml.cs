namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class AuthenticateView
	{
		public static AuthenticateView Create()
		{
			return Create<AuthenticateView>();
		}

		public AuthenticateView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
