using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class NameEntryStepView : BaseOnboardingView
	{
		public static NameEntryStepView Create()
		{
			return Create<NameEntryStepView>();
		}

		public NameEntryStepView(NameEntryOnboardingStepViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = viewModel;
		}
	}
}
