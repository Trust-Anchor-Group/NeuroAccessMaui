using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class AccountSetupView : BaseOnboardingView
	{
		public static AccountSetupView Create()
		{
			return Create<AccountSetupView>();
		}

		public AccountSetupView(AccountSetupOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
