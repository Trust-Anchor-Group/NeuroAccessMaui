using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups.Pin
{
	/// <summary>
	/// View model for page letting the user enter a PIN to be verified with the PIN defined by the user earlier.
	/// </summary>
	public partial class CheckPinViewModel : ReturningPopupViewModel<string>
	{

		/// <summary>
		/// PIN text entry
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(EnterPinCommand))]
		private string pinText = string.Empty;

		/// <summary>
		/// If PIN can be entered
		/// </summary>
		public bool CanEnterPin => !string.IsNullOrEmpty(this.PinText);

		/// <summary>
		/// Enters the PIN provided by the user.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEnterPin))]
		private async Task EnterPin()
		{
			if (!string.IsNullOrEmpty(this.PinText))
			{
				string Pin = this.PinText;

				if (await App.CheckPinAndUnblockUser(Pin))
				{
					this.result.TrySetResult(Pin);
					await ServiceRef.UiService.PopAsync();
				}
				else
				{
					this.PinText = string.Empty;

					long PinAttemptCounter = await App.GetCurrentPinCounter();
					long RemainingAttempts = Math.Max(0, Constants.Pin.FirstMaxPinAttempts - PinAttemptCounter);

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PinIsInvalid), RemainingAttempts]);

					await App.CheckUserBlocking();
				}
			}
		}

		/// <summary>
		/// Cancels PIN-entry
		/// </summary>
		[RelayCommand]
		private async Task Cancel()
		{
			this.result.TrySetResult(null);
			await ServiceRef.UiService.PopAsync();
		}
	}
}
