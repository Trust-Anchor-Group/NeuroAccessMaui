using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Popups.Pin;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using System.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Main.Settings
{
	/// <summary>
	/// The view model to bind to for when displaying security options.
	/// </summary>
	public partial class SettingsViewModel : XmppViewModel
	{
		private readonly bool initializing = false;

		/// <summary>
		/// Creates an instance of the <see cref="SettingsViewModel"/> class.
		/// </summary>
		public SettingsViewModel()
		{
			this.CanProhibitScreenCapture = ServiceRef.PlatformSpecific.CanProhibitScreenCapture;
			this.CanEnableScreenCapture = ServiceRef.PlatformSpecific.ProhibitScreenCapture;
			this.CanDisableScreenCapture = !this.CanEnableScreenCapture;

			this.initializing = true;
			try
			{
				switch (DisplayMode)
				{
					case AppTheme.Light:
						this.IsLightMode = true;
						break;

					case AppTheme.Dark:
						this.IsDarkMode = true;
						break;
				}
			}
			finally
			{
				this.initializing = false;
			}
		}

		#region Properties

		/// <summary>
		/// If screen capture prohibition can be controlled
		/// </summary>
		[ObservableProperty]
		private bool canProhibitScreenCapture;

		/// <summary>
		/// Gets or sets whether the identity is approved or not.
		/// </summary>
		[ObservableProperty]
		private bool canEnableScreenCapture;

		/// <summary>
		/// Gets or sets whether the identity is approved or not.
		/// </summary>
		[ObservableProperty]
		private bool canDisableScreenCapture;

		/// <summary>
		/// Gets or sets whether the current display mode is Light Mode.
		/// </summary>
		[ObservableProperty]
		private bool isLightMode;

		/// <summary>
		/// Gets or sets whether the current display mode is Dark Mode.
		/// </summary>
		[ObservableProperty]
		private bool isDarkMode;

		/// <summary>
		/// Current display mode
		/// </summary>
		public static AppTheme DisplayMode
		{
			get
			{
				AppTheme? Result = Application.Current?.UserAppTheme;

				if (!Result.HasValue || Result.Value == AppTheme.Unspecified)
					Result = Application.Current?.PlatformAppTheme;

				return Result ?? AppTheme.Unspecified;
			}
		}

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(this.IsLightMode):
					if (!this.initializing)
						SetTheme(this.IsLightMode ? AppTheme.Light : AppTheme.Dark);
					break;

				case nameof(this.IsDarkMode):
					if (!this.initializing)
						SetTheme(this.IsDarkMode ? AppTheme.Dark : AppTheme.Light);
					break;
			}
		}

		private static void SetTheme(AppTheme Theme)
		{
			ServiceRef.TagProfile.SetTheme(Theme);
		}

		#endregion

		#region Commands

		[RelayCommand]
		internal static async Task ChangePin()
		{
			try
			{
				while (true)
				{
					ChangePinPage Page = ServiceHelper.GetService<ChangePinPage>();
					await MopupService.Instance.PushAsync(Page);

					(string OldPin, string NewPin) = await Page.Result;

					if (OldPin is null || OldPin == NewPin)
						return;

					if (!ServiceRef.TagProfile.HasPin ||
						ServiceRef.TagProfile.ComputePinHash(OldPin) == ServiceRef.TagProfile.PinHash)
					{
						string NewPassword = ServiceRef.CryptoService.CreateRandomPassword();

						if (!await ServiceRef.XmppService.ChangePassword(NewPassword))
						{
							await ServiceRef.UiSerializer.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
								ServiceRef.Localizer[nameof(AppResources.UnableToChangePassword)]);
							return;
						}

						ServiceRef.TagProfile.Pin = NewPin;
						ServiceRef.TagProfile.SetAccount(ServiceRef.TagProfile.Account!, NewPassword, string.Empty);

						await ServiceRef.UiSerializer.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.PinChanged)]);
						return;
					}

					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PinIsInvalid)]);

					// TODO: Limit number of attempts.
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		[RelayCommand]
		private async Task PermitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
				return;

			if (!await App.VerifyPin())
				return;

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = false;
			this.CanEnableScreenCapture = false;
			this.CanDisableScreenCapture = true;
		}

		[RelayCommand]
		private async Task ProhibitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
				return;

			if (!await App.VerifyPin())
				return;

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = true;
			this.CanEnableScreenCapture = true;
			this.CanDisableScreenCapture = false;
		}

		[RelayCommand]
		private static Task GoBack()
		{
			return ServiceRef.NavigationService.GoBackAsync();
		}

		#endregion
	}
}
