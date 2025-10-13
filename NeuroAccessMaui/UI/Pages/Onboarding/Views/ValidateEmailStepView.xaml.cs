using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class ValidateEmailStepView : BaseOnboardingView
	{
		public static ValidateEmailStepView Create()
		{
			return Create<ValidateEmailStepView>();
		}

		public ValidateEmailStepView(ValidateEmailOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
