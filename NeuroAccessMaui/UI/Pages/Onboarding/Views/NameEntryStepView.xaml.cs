using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class NameEntryStepView : BaseOnboardingView
	{
		/// <summary>
		/// Initializes a new presentation-only instance of the <see cref="NameEntryStepView"/> class.
		/// </summary>
		public NameEntryStepView()
		{
			this.InitializeComponent();
		}

		public static NameEntryStepView Create()
		{
			return Create<NameEntryStepView>();
		}

		public NameEntryStepView(NameEntryOnboardingStepViewModel viewModel) : this()
		{
			this.ContentViewModel = viewModel;
		}
	}
}
