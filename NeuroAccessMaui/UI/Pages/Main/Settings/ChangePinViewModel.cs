using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Main.Settings
{
	/// <summary>
	/// View model for the <see cref="ChangePinPage"/> page.
	/// </summary>
	public partial class ChangePinViewModel : XmppViewModel
	{
		/// <summary>
		/// View model for the <see cref="ChangePinPage"/> page.
		/// </summary>
		public ChangePinViewModel()
		{
		}

		/// <summary>
		/// Old PIN text entry
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
		private string oldPin = string.Empty;

		/// <summary>
		/// New PIN text entry
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
		private string newPin = string.Empty;

		/// <summary>
		/// New PIN text entry, retyped
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
		private string newPin2 = string.Empty;

		/// <summary>
		/// If new PIN is OK
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
		private bool incorrectPinAlertShown = false;

		/// <summary>
		/// If new PIN can be used.
		/// </summary>
		public bool CanChangePin => this.NewPinStrength == PinStrength.Strong && this.NewPinMatchesRetypedNewPin;

		/// <summary>
		/// Strength of new PIN
		/// </summary>
		public PinStrength NewPinStrength => ServiceRef.TagProfile.ValidatePinStrength(this.NewPin);

		/// <summary>
		/// If both new PINs match.
		/// </summary>
		public bool NewPinMatchesRetypedNewPin => string.IsNullOrEmpty(this.NewPin) ? string.IsNullOrEmpty(this.NewPin2) : string.Equals(this.NewPin, this.NewPin2, StringComparison.Ordinal);

		/// <summary>
		/// Tries to change the PIN
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanChangePin))]
		private async Task ChangePin()
		{
			if (!string.IsNullOrEmpty(this.OldPin) && this.CanChangePin)
			{
				if (await App.CheckPinAndUnblockUser(this.OldPin))
				{
					try
					{
						string NewPassword = ServiceRef.CryptoService.CreateRandomPassword();

						if (!await ServiceRef.XmppService.ChangePassword(NewPassword))
						{
							await ServiceRef.UiSerializer.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
								ServiceRef.Localizer[nameof(AppResources.UnableToChangePassword)]);
							return;
						}

						ServiceRef.TagProfile.Pin = this.NewPin;
						ServiceRef.TagProfile.SetAccount(ServiceRef.TagProfile.Account!, NewPassword, string.Empty);

						await ServiceRef.UiSerializer.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.PinChanged)]);

						await ServiceRef.NavigationService.GoBackAsync();
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
						await ServiceRef.UiSerializer.DisplayException(ex);
					}
				}
				else
				{
					this.OldPin = string.Empty;

					long PinAttemptCounter = await App.GetCurrentPinCounter();
					long RemainingAttempts = Math.Max(0, Constants.Pin.FirstMaxPinAttempts - PinAttemptCounter);

					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PinIsInvalid), RemainingAttempts]);

					await App.CheckUserBlocking();
				}
			}
		}

		/// <summary>
		/// Goes back to the previous page.
		/// </summary>
		[RelayCommand]
		private static Task GoBack()
		{
			return ServiceRef.NavigationService.GoBackAsync();
		}
	}
}
