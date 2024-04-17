using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Password
{
	/// <summary>
	/// View model for page letting the user enter a password to be verified with the password defined by the user earlier.
	/// </summary>
	public partial class CheckPasswordViewModel : ReturningPopupViewModel<string>
	{
		/// <summary>
		/// Password text entry
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(EnterPasswordCommand))]
		private string passwordText = string.Empty;

		/// <summary>
		/// If password can be entered
		/// </summary>
		public bool CanEnterPassword => !string.IsNullOrEmpty(this.PasswordText);

		/// <summary>
		/// Enters the password provided by the user.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanEnterPassword))]
		private async Task EnterPassword()
		{
			if (!string.IsNullOrEmpty(this.PasswordText))
			{
				string Password = this.PasswordText;

				if (await App.CheckPasswordAndUnblockUser(Password))
				{
					this.result.TrySetResult(Password);
					await ServiceRef.UiService.PopAsync();
				}
				else
				{
					this.PasswordText = string.Empty;

					long PasswordAttemptCounter = await App.GetCurrentPasswordCounter();
					long RemainingAttempts = Math.Max(0, Constants.Password.FirstMaxPasswordAttempts - PasswordAttemptCounter);

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PasswordIsInvalid), RemainingAttempts]);

					await App.CheckUserBlocking();
				}
			}
		}

		/// <summary>
		/// Cancels password-entry
		/// </summary>
		[RelayCommand]
		private async Task Cancel()
		{
			this.result.TrySetResult(null);
			await ServiceRef.UiService.PopAsync();
		}
	}
}
