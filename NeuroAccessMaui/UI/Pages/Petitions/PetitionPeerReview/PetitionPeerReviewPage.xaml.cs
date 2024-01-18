using CommunityToolkit.Maui.Layouts;

namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview
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

			ViewModel.AddView(ReviewStep.Photo, this.PhotoView);
			ViewModel.AddView(ReviewStep.NamePnr, this.NamePnrView);
			ViewModel.AddView(ReviewStep.PersonalInfo, this.PersonalInfoView);
			ViewModel.AddView(ReviewStep.OrganizationalInfo, this.OrganizationalInfoView);
			ViewModel.AddView(ReviewStep.Consent, this.ConsentView);
			ViewModel.AddView(ReviewStep.Authenticate, this.AuthenticateView);
			ViewModel.AddView(ReviewStep.Approved, this.ApprovedView);

			StateContainer.SetCurrentState(this.GridWithAnimation, nameof(ReviewStep.Photo));

			ViewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		/// <summary>
		/// Destroys the page.
		/// </summary>
		~PetitionPeerReviewPage()
		{
			this.ContentPageModel.PropertyChanged -= this.ViewModel_PropertyChanged;
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(PetitionPeerReviewViewModel.CurrentStep):
					StateContainer.SetCurrentState(this.GridWithAnimation, (this.ContentPageModel as PetitionPeerReviewViewModel)?.CurrentStep.ToString());
					break;
			}
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			//!!! this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}
	}
}
