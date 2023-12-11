using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Popups.Pin;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main.Security
{
	/// <summary>
	/// The view model to bind to for when displaying security options.
	/// </summary>
	public partial class SecurityViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="SecurityViewModel"/> class.
		/// </summary>
		public SecurityViewModel()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.CanProhibitScreenCapture = ServiceRef.PlatformSpecific.CanProhibitScreenCapture;
			this.CanEnableScreenCapture = ServiceRef.PlatformSpecific.ProhibitScreenCapture;
			this.CanDisableScreenCapture = !this.CanEnableScreenCapture;
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

		#endregion

		#region Commands

		internal static async Task ChangePinAsync()
		{
			try
			{
				while (true)
				{
					ChangePinPage Page = ServiceHelper.GetService<ChangePinPage>();
					await MopupService.Instance.PushAsync(Page);

					(string OldPin, string NewPin) = await Page.Result;

					if (OldPin is null || OldPin == NewPin)
					{
						return;
					}

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
						ServiceRef.TagProfile.SetAccount(ServiceRef.TagProfile.Account, NewPassword, string.Empty);

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
		private Task ChangePin()
		{
			return ChangePinAsync();
		}

		[RelayCommand]
		private async Task PermitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
			{
				return;
			}

			if (!await App.VerifyPin())
			{
				return;
			}

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = false;
			this.CanEnableScreenCapture = false;
			this.CanDisableScreenCapture = true;
		}

		[RelayCommand]
		private async Task ProhibitScreenCapture()
		{
			if (!ServiceRef.PlatformSpecific.CanProhibitScreenCapture)
			{
				return;
			}

			if (!await App.VerifyPin())
			{
				return;
			}

			ServiceRef.PlatformSpecific.ProhibitScreenCapture = true;
			this.CanEnableScreenCapture = true;
			this.CanDisableScreenCapture = false;
		}

		#endregion
	}
}
