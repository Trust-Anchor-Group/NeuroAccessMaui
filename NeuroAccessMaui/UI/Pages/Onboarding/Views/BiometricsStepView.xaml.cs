using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Onboarding.Views
{
	public partial class BiometricsStepView : BaseOnboardingView
	{
		/// <summary>
		/// Initializes a new presentation-only instance of the <see cref="BiometricsStepView"/> class.
		/// </summary>
		public BiometricsStepView()
		{
			this.InitializeComponent();
		}

		public static BiometricsStepView Create()
		{
			return Create<BiometricsStepView>();
		}

		public BiometricsStepView(BiometricsOnboardingStepViewModel viewModel) : this()
		{
			this.ContentViewModel = viewModel;
		}
	}
}
