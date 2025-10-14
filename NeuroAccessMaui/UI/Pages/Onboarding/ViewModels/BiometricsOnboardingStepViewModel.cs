using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Onboarding step for enabling biometrics.
	/// </summary>
	public partial class BiometricsOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private readonly BiometricMethod biometricMethod = ServiceRef.PlatformSpecific.GetBiometricMethod();

		public BiometricsOnboardingStepViewModel() : base(OnboardingStep.Biometrics) { }

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingBiometricsPageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingBiometricsPageDetails)];

		public bool IsFingerprint => this.biometricMethod is BiometricMethod.Fingerprint or BiometricMethod.TouchId or BiometricMethod.Unknown;
		public bool IsFace => this.biometricMethod is BiometricMethod.Face or BiometricMethod.FaceId or BiometricMethod.Unknown;

		[RelayCommand]
		private void Later()
		{
			ServiceRef.TagProfile.AuthenticationMethod = AuthenticationMethod.Password;
			this.CoordinatorViewModel?.GoToStepCommand.Execute(OnboardingStep.Finalize);
		}

		[RelayCommand]
		private void Enable()
		{
			ServiceRef.TagProfile.AuthenticationMethod = AuthenticationMethod.Fingerprint;
			this.CoordinatorViewModel?.GoToStepCommand.Execute(OnboardingStep.Finalize);
		}

		internal override Task<bool> OnNextAsync() => Task.FromResult(true);
	}
}
