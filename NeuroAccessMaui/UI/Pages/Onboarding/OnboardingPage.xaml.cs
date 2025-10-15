using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	public partial class OnboardingPage : BaseContentPage
	{
		public OnboardingPage(OnboardingViewModel viewModel)
		{
			this.InitializeComponent();

			this.ContentPageModel = viewModel;

			foreach (ViewSwitcherStateView stateView in this.FlowSwitcher.StateViews)
			{
				if (stateView.Content is BaseOnboardingView onboardingView)
				{
					BaseOnboardingStepViewModel stepViewModel = onboardingView.ViewModel<BaseOnboardingStepViewModel>();
					viewModel.RegisterStep(stepViewModel);
				}
			}
		}
	}
}
