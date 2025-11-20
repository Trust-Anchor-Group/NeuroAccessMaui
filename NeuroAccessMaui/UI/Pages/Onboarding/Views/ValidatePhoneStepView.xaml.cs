using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class ValidatePhoneStepView : BaseOnboardingView
	{
		public static ValidatePhoneStepView Create()
		{
			return Create<ValidatePhoneStepView>();
		}

		public ValidatePhoneStepView(ValidatePhoneOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
