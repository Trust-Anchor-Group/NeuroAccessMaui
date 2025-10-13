using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class BiometricsStepView : BaseOnboardingView
	{
		public static BiometricsStepView Create()
		{
			return Create<BiometricsStepView>();
		}

		public BiometricsStepView(BiometricsOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
