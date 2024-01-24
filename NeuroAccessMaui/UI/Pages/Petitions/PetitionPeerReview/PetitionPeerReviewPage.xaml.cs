using CommunityToolkit.Maui.Layouts;
using NeuroAccessMaui.Services;

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
			ViewModel.AddView(ReviewStep.Name, this.NameView);
			ViewModel.AddView(ReviewStep.Pnr, this.PnrView);
			ViewModel.AddView(ReviewStep.Nationality, this.NationalityView);
			ViewModel.AddView(ReviewStep.BirthDate, this.BirthDateView);
			ViewModel.AddView(ReviewStep.Gender, this.GenderView);
			ViewModel.AddView(ReviewStep.PersonalAddressInfo, this.PersonalAddressInfoView);
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

		private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			try
			{
				switch (e.PropertyName)
				{
					case nameof(PetitionPeerReviewViewModel.CurrentStep):
						DateTime Start = DateTime.Now;

						while (!StateContainer.GetCanStateChange(this.GridWithAnimation) && DateTime.Now.Subtract(Start).TotalSeconds < 2)
							await Task.Delay(100);

						await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation,
							(this.ContentPageModel as PetitionPeerReviewViewModel)?.CurrentStep.ToString(),
							new Animation()
							{
								{
									0,
									1,
									new Animation((p)=>
									{
										this.GridWithAnimation.Scale = p / 5 + 0.8;
										this.GridWithAnimation.Opacity = p;
									}, 1, 0, Easing.CubicIn)
								}
							},
							new Animation()
							{
								{
									0,
									1,
									new Animation((p)=>
									{
										this.GridWithAnimation.Scale = p / 5 + 0.8;
										this.GridWithAnimation.Opacity = p;
									}, 0, 1, Easing.CubicInOut)
								}
							},
							CancellationToken.None);
						break;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
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
