using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class FinalizeStepView : BaseOnboardingView
	{
		/// <summary>
		/// Initializes a new presentation-only instance of the <see cref="FinalizeStepView"/> class.
		/// </summary>
		public FinalizeStepView()
		{
			this.InitializeComponent();
		}

		public static FinalizeStepView Create()
		{
			return Create<FinalizeStepView>();
		}

		public FinalizeStepView(FinalizeOnboardingStepViewModel viewModel) : this()
		{
			this.ContentViewModel = viewModel;
		}
	}
}
