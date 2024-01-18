namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views
{
	public partial class PersonalAddressInfoView
	{
		public static PersonalAddressInfoView Create()
		{
			return Create<PersonalAddressInfoView>();
		}

		public PersonalAddressInfoView(PetitionPeerReviewViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
