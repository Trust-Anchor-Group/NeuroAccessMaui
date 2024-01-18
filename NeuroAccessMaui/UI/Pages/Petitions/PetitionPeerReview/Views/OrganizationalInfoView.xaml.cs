namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class OrganizationalInfoView
	{
		public static OrganizationalInfoView Create()
		{
			return Create<OrganizationalInfoView>();
		}

		public OrganizationalInfoView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
