using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.UI.Popups.Info;
using Waher.Script.Content.Functions.InputOutput;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class BiometricsViewModel() : BaseRegistrationViewModel(RegistrationStep.Biometrics)
	{
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
			this.OnPropertyChanged(nameof(this.WhatIsX));
		}

		private readonly BiometricMethod biometricMethod = ServiceRef.PlatformSpecific.GetBiometricMethod();

		/// <summary>
		/// Gets the size of the background for the authentication method.
		/// </summary>
		public double AuthenticationBackgroundSize => 120.0;

		/// <summary>
		/// Gets the size of the background for the authentication method.
		/// </summary>
		public double AuthenticationBackgroundCornerRadius => this.AuthenticationBackgroundSize / 2;
		/// <summary>
		/// Gets the size of the icon for the authentication method.
		/// </summary>
		public double AuthenticationIconSize => 60.0;

		/// <summary>
		/// Gets a value indicating whether the device supports fingerprint authentication.
		/// </summary>
		public bool IsFingerprint => this.biometricMethod is BiometricMethod.Fingerprint or BiometricMethod.TouchId or BiometricMethod.Unknown;

		/// <summary>
		/// Gets a value indicating whether the device supports face authentication.
		/// </summary>
		public bool IsFace => this.biometricMethod is BiometricMethod.Face or BiometricMethod.FaceId or BiometricMethod.Unknown;

		/// <summary>
		/// Gets the text describing what authentication to enable
		/// </summary>
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

		/// <summary>
		/// Gets the text for the info button
		/// Ex: What is Touch ID?
		/// </summary>
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

			ShowInfoPopup infoPage = new(this.WhatIsX, message);
			await ServiceRef.UiService.PushAsync(infoPage);
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
