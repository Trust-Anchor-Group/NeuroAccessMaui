using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.Shapes;
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
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			LocalizationManager.Current.PropertyChanged += this.Localization_Changed;
		}

		/// <inheritdoc />
		public override async Task OnDisposeAsync()
		{
			LocalizationManager.Current.PropertyChanged -= this.Localization_Changed;

			await base.OnDisposeAsync();
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
		public RoundRectangle AuthenticationBackgroundStrokeShape => new RoundRectangle { CornerRadius = this.AuthenticationBackgroundSize / 2 };
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

				string MethodName = this.biometricMethod switch
				{
					BiometricMethod.TouchId => ServiceRef.Localizer[nameof(AppResources.TouchId)],
					BiometricMethod.FaceId => ServiceRef.Localizer[nameof(AppResources.FaceId)],
					_ => ServiceRef.Localizer[nameof(AppResources.BiometricAuthentication)].ToString().ToLower(CultureInfo.CurrentUICulture)
				};

				return ServiceRef.Localizer[nameof(AppResources.OnboardingBiometricsPageDetails), MethodName];
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
				string Subject = this.biometricMethod switch
				{
					BiometricMethod.TouchId => ServiceRef.Localizer[nameof(AppResources.TouchId)],
					BiometricMethod.FaceId => ServiceRef.Localizer[nameof(AppResources.FaceId)],
					_ => ServiceRef.Localizer[nameof(AppResources.BiometricAuthentication)].ToString().ToLower(CultureInfo.CurrentUICulture)
				};

				return ServiceRef.Localizer[nameof(AppResources.WhatIsX), Subject];
			}
		}

		[RelayCommand]
		private async Task ShowBiometricsInfo()
		{
			string Message = this.biometricMethod switch
			{
				BiometricMethod.FaceId => ServiceRef.Localizer[nameof(AppResources.FaceIdInfo)],
				BiometricMethod.TouchId => ServiceRef.Localizer[nameof(AppResources.TouchIdInfo)],
				_ => ServiceRef.Localizer[nameof(AppResources.BiometricAuthenticationInfo)]
			};

			ShowInfoPopup InfoPage = new(this.WhatIsX, Message);
			await ServiceRef.PopupService.PushAsync(InfoPage);
		}

		[RelayCommand]
		private void Later()
		{
			ServiceRef.TagProfile.AuthenticationMethod = AuthenticationMethod.Password;
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
