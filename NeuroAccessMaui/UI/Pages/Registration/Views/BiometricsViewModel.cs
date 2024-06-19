using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Main.Settings;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
    public partial class BiometricsViewModel : BaseRegistrationViewModel
	{
		public BiometricsViewModel()
			: base(RegistrationStep.Biometrics)
		{
		
		}

				/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LocalizationManager.Current.PropertyChanged += this.Localization_Changed;
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			LocalizationManager.Current.PropertyChanged -= this.Localization_Changed;

			await base.OnDispose();
		}


		private void Localization_Changed(object? Sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.DetailText));
		}

		/// <summary>
		/// Gets the text describing what authentication to enable
		/// </summary>
		public string DetailText
		{
			get {
			#if ANDROID
				string? bio1 = ServiceRef.Localizer["FaceRecognition"];
				string? bio2 = ServiceRef.Localizer["FingerprintRecognition"];
				return ServiceRef.Localizer["OnboardingBiometricsPageDetails", bio1, bio2];
			#else
				return ServiceRef.Localizer["OnboardingBiometricsPageDetails", "Face ID", "Touch ID"];
			#endif
			}
		}

        [RelayCommand]
		private void Later()
		{
			GoToRegistrationStep(RegistrationStep.Finalize);
		}

		[RelayCommand]
		private void Enable()
		{
			ServiceRef.TagProfile.AuthenticationMethod = AuthenticationMethod.Fingerprint;
			GoToRegistrationStep(RegistrationStep.Finalize);
		}
    }
}