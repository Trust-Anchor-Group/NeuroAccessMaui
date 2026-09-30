using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class DefinePasswordStepView : BaseOnboardingView
	{
		/// <summary>
		/// Initializes a new presentation-only instance of the <see cref="DefinePasswordStepView"/> class.
		/// </summary>
		public DefinePasswordStepView()
		{
			this.InitializeComponent();
		}

		public static DefinePasswordStepView Create()
		{
			return Create<DefinePasswordStepView>();
		}

		public DefinePasswordStepView(DefinePasswordOnboardingStepViewModel viewModel) : this()
		{
			this.ContentViewModel = viewModel;
		}
	}
}
