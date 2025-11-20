using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class WelcomeStepView : BaseOnboardingView
	{
		public static WelcomeStepView Create()
		{
			return Create<WelcomeStepView>();
		}

		public WelcomeStepView(WelcomeOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
