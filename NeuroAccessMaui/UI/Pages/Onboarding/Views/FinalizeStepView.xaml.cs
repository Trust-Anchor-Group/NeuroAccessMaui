using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class FinalizeStepView : BaseOnboardingView
	{
		public static FinalizeStepView Create()
		{
			return Create<FinalizeStepView>();
		}

		public FinalizeStepView(FinalizeOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
