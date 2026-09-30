using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class WelcomeStepView : BaseOnboardingView
	{
		/// <summary>
		/// Initializes a new presentation-only instance of the <see cref="WelcomeStepView"/> class.
		/// </summary>
		public WelcomeStepView()
		{
			this.InitializeComponent();
		}

		public static WelcomeStepView Create()
		{
			return Create<WelcomeStepView>();
		}

		public WelcomeStepView(WelcomeOnboardingStepViewModel viewModel) : this()
		{
			this.ContentViewModel = viewModel;
		}
	}
}
