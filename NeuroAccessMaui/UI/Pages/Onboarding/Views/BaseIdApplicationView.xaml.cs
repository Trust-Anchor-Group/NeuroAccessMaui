using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class BaseIdApplicationView : BaseOnboardingView
	{
		public static BaseIdApplicationView Create()
		{
			return Create<BaseIdApplicationView>();
		}

		public BaseIdApplicationView(BaseIdApplicationOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
