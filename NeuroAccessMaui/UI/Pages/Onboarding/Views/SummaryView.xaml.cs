using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class SummaryView : BaseOnboardingView
	{
		public static SummaryView Create()
		{
			return Create<SummaryView>();
		}

		public SummaryView(SummaryOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
