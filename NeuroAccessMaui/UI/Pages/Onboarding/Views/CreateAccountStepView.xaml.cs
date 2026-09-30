using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class CreateAccountStepView : BaseOnboardingView
	{
		/// <summary>
		/// Initializes a new presentation-only instance of the <see cref="CreateAccountStepView"/> class.
		/// </summary>
		public CreateAccountStepView()
		{
			this.InitializeComponent();
		}

		public static CreateAccountStepView Create()
		{
			return Create<CreateAccountStepView>();
		}

		public CreateAccountStepView(CreateAccountOnboardingStepViewModel viewModel) : this()
		{
			this.ContentViewModel = viewModel;
		}
	}
}
