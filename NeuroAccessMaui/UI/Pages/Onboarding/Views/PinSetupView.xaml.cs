using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class PinSetupView : BaseOnboardingView
	{
		public static PinSetupView Create()
		{
			return Create<PinSetupView>();
		}

		public PinSetupView(PinSetupOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
