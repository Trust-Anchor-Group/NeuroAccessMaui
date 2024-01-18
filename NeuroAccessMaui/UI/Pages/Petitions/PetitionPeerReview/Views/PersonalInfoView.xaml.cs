namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class PersonalInfoView
	{
		public static PersonalInfoView Create()
		{
			return Create<PersonalInfoView>();
		}

		public PersonalInfoView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
