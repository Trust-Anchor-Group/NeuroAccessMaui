using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class DefinePasswordStepView : BaseOnboardingView
	{
		public static DefinePasswordStepView Create()
		{
			return Create<DefinePasswordStepView>();
		}

		public DefinePasswordStepView(DefinePasswordOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
