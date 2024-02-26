using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Registration.Views;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages.Main.ChangePin
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
			: base()
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
		[NotifyPropertyChangedFor(nameof(IsPin1NotValid))]
		[NotifyPropertyChangedFor(nameof(IsPin2NotValid))]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		[NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
		private string newPin = string.Empty;

		/// <summary>
		/// New PIN text entry, retyped
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsPin2NotValid))]
		[NotifyPropertyChangedFor(nameof(NewPinsMatch))]
		[NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
		private string newPin2 = string.Empty;

		/// <summary>
		/// If new PIN is OK
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(NewPinsMatch))]
		[NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
		private bool incorrectPinAlertShown = false;

		/// <summary>
		/// If new PIN can be used.
		/// </summary>
		public bool CanChangePin => this.NewPinStrength == PinStrength.Strong && this.NewPinMatchesRetypedNewPin && this.IsConnected && !this.IsBusy;

		/// <summary>
		/// Strength of new PIN
		/// </summary>
		public PinStrength NewPinStrength => ServiceRef.TagProfile.ValidatePinStrength(this.NewPin);

		/// <summary>
		/// Gets the value indicating whether the entered <see cref="NewPin"/> is the same as the entered <see cref="NewPin2"/>.
		/// </summary>
		public bool NewPinsMatch => string.IsNullOrEmpty(this.NewPin) ? string.IsNullOrEmpty(this.NewPin2) : string.Equals(this.NewPin, this.NewPin2, StringComparison.Ordinal);

		/// <summary>
		/// If First New PIN entry is not valid.
		/// </summary>
		public bool IsPin1NotValid => !string.IsNullOrEmpty(this.NewPin) && this.NewPinStrength != PinStrength.Strong;

		/// <summary>
		/// If Second New PIN entry is not valid.
		/// </summary>
		public bool IsPin2NotValid => !string.IsNullOrEmpty(this.NewPin2) && !this.NewPinsMatch;

		/// <summary>
		/// If both new PINs match.
		/// </summary>
		public bool NewPinMatchesRetypedNewPin => string.IsNullOrEmpty(this.NewPin) ? string.IsNullOrEmpty(this.NewPin2) : string.Equals(this.NewPin, this.NewPin2, StringComparison.Ordinal);

		/// <summary>
		/// Localized validation error message.
		/// </summary>
		public string LocalizedValidationError => DefinePinViewModel.GetLocalizedValidationError(this.NewPinStrength);

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			this.NotifyCommandsCanExecuteChanged();
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			return MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await base.XmppService_ConnectionStateChanged(Sender, NewState);

				this.NotifyCommandsCanExecuteChanged();
			});
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		private void NotifyCommandsCanExecuteChanged()
		{
			this.ChangePinCommand.NotifyCanExecuteChanged();
		}

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
							await ServiceRef.UiService.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
								ServiceRef.Localizer[nameof(AppResources.UnableToChangePassword)]);
							return;
						}

						ServiceRef.TagProfile.Pin = this.NewPin;
						ServiceRef.TagProfile.SetAccount(ServiceRef.TagProfile.Account!, NewPassword, string.Empty);

						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.PinChanged)]);

						await this.GoBack();
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
						await ServiceRef.UiService.DisplayException(ex);
					}
				}
				else
				{
					this.OldPin = string.Empty;

					long PinAttemptCounter = await App.GetCurrentPinCounter();
					long RemainingAttempts = Math.Max(0, Constants.Pin.FirstMaxPinAttempts - PinAttemptCounter);

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PinIsInvalid), RemainingAttempts]);

					await App.CheckUserBlocking();
				}
			}
		}
	}
}
