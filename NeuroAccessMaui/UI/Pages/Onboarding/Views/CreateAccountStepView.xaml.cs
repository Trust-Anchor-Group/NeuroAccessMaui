using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class CreateAccountStepView : BaseOnboardingView
	{
		public static CreateAccountStepView Create()
		{
			return Create<CreateAccountStepView>();
		}

		public CreateAccountStepView(CreateAccountOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
