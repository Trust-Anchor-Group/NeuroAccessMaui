using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups.Info;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
    public partial class BiometricsOnboardingStepViewModel : BaseOnboardingStepViewModel
    {
        private readonly BiometricMethod biometricMethod = ServiceRef.PlatformSpecific.GetBiometricMethod();

        public BiometricsOnboardingStepViewModel() : base(OnboardingStep.Biometrics) { }

        public override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();
            LocalizationManager.Current.PropertyChanged += this.Localization_Changed;
        }

        public override async Task OnDisposeAsync()
        {
            LocalizationManager.Current.PropertyChanged -= this.Localization_Changed;
            await base.OnDisposeAsync();
        }

        private void Localization_Changed(object? sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.DetailText));
            this.OnPropertyChanged(nameof(this.WhatIsX));
        }

        public double AuthenticationBackgroundSize => 120.0;

        public double AuthenticationBackgroundCornerRadius => this.AuthenticationBackgroundSize / 2;

        public double AuthenticationIconSize => 60.0;

        public bool IsFingerprint => this.biometricMethod is BiometricMethod.Fingerprint or BiometricMethod.TouchId or BiometricMethod.Unknown;

        public bool IsFace => this.biometricMethod is BiometricMethod.Face or BiometricMethod.FaceId or BiometricMethod.Unknown;

        public string DetailText
        {
            get
            {
                string methodName = this.biometricMethod switch
                {
                    BiometricMethod.TouchId => ServiceRef.Localizer[nameof(AppResources.TouchId)],
                    BiometricMethod.FaceId => ServiceRef.Localizer[nameof(AppResources.FaceId)],
                    _ => ServiceRef.Localizer[nameof(AppResources.BiometricAuthentication)].ToString().ToLower(CultureInfo.CurrentUICulture)
                };

                return ServiceRef.Localizer[nameof(AppResources.OnboardingBiometricsPageDetails), methodName];
            }
        }

        public string WhatIsX
        {
            get
            {
                string subject = this.biometricMethod switch
                {
                    BiometricMethod.TouchId => ServiceRef.Localizer[nameof(AppResources.TouchId)],
                    BiometricMethod.FaceId => ServiceRef.Localizer[nameof(AppResources.FaceId)],
                    _ => ServiceRef.Localizer[nameof(AppResources.BiometricAuthentication)].ToString().ToLower(CultureInfo.CurrentUICulture)
                };

                return ServiceRef.Localizer[nameof(AppResources.WhatIsX), subject];
            }
        }

        [RelayCommand]
        private async Task ShowBiometricsInfo()
        {
            string message = this.biometricMethod switch
            {
                BiometricMethod.FaceId => ServiceRef.Localizer[nameof(AppResources.FaceIdInfo)],
                BiometricMethod.TouchId => ServiceRef.Localizer[nameof(AppResources.TouchIdInfo)],
                _ => ServiceRef.Localizer[nameof(AppResources.BiometricAuthenticationInfo)]
            };

            ShowInfoPopup popup = new(this.WhatIsX, message);
            await ServiceRef.PopupService.PushAsync(popup);
        }

        [RelayCommand]
        private async Task Later()
        {
            ServiceRef.TagProfile.AuthenticationMethod = AuthenticationMethod.Password;
            if (this.CoordinatorViewModel is not null)
                await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.Finalize);
        }

        [RelayCommand]
        private async Task Enable()
        {
            ServiceRef.TagProfile.AuthenticationMethod = AuthenticationMethod.Fingerprint;
            if (this.CoordinatorViewModel is not null)
                await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.Finalize);
        }
    }
}
