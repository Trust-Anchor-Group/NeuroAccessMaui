using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// ViewModel for final step in onboarding.
	/// </summary>
	public class FinalizeOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public FinalizeOnboardingStepViewModel() : base(OnboardingStep.Finalize) { }

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingFinalizePageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.SuccessTitle)];

		internal override Task<bool> OnNextAsync()
		{
			ServiceRef.LogService.LogInformational("Onboarding finalize complete (simulated)." );
			return Task.FromResult(true);
		}
	}
}
