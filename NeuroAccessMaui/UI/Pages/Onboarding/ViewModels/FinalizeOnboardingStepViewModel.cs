using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Final step: navigate into main application shell.
	/// </summary>
	public class FinalizeOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public FinalizeOnboardingStepViewModel() : base(OnboardingStep.Finalize) { }

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingFinalizePageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.SuccessTitle)];

		internal override async Task<bool> OnNextAsync()
		{
			await App.SetMainPageAsync();
			return true;
		}
	}
}
