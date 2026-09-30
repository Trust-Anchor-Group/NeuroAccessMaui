using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class ValidateEmailStepView : BaseOnboardingView
	{
		/// <summary>
		/// Initializes a new presentation-only instance of the <see cref="ValidateEmailStepView"/> class.
		/// </summary>
		public ValidateEmailStepView()
		{
			this.InitializeComponent();
		}

		public static ValidateEmailStepView Create()
		{
			return Create<ValidateEmailStepView>();
		}

		public ValidateEmailStepView(ValidateEmailOnboardingStepViewModel viewModel) : this()
		{
			this.ContentViewModel = viewModel;
		}
	}
}
