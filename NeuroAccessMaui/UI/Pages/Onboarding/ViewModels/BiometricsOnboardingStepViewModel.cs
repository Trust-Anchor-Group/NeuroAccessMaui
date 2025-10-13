using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// ViewModel for enabling biometrics in onboarding.
	/// </summary>
	public partial class BiometricsOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private bool enabled;
		public BiometricsOnboardingStepViewModel() : base(OnboardingStep.Biometrics) { }

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingBiometricsPageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingBiometricsPageDetails)];

		[RelayCommand]
		private void EnableBiometrics()
		{
			this.enabled = true;
			ServiceRef.LogService.LogInformational("Biometrics enabled (simulated)." );
		}

		internal override Task<bool> OnNextAsync() => Task.FromResult(true);
	}
}
