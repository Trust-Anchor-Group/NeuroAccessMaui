using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// ViewModel for validating phone in onboarding.
	/// </summary>
	public partial class ValidatePhoneOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private bool codeSent;

		public ValidatePhoneOnboardingStepViewModel() : base(OnboardingStep.ValidatePhone) { }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSend))]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private string phoneNumber = string.Empty;

		[ObservableProperty]
		private bool isBusy;

		[ObservableProperty]
		private string errorMessage = string.Empty;

		public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);
		public bool CanSend => !this.IsBusy && !string.IsNullOrWhiteSpace(this.PhoneNumber);
		public bool CanContinue => this.codeSent;

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingPhonePageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingPhonePageDetails)];

		[RelayCommand(CanExecute = nameof(CanSend))]
		private async Task SendCode()
		{
			this.IsBusy = true;
			this.ErrorMessage = string.Empty;
			try
			{
				if (!ServiceRef.NetworkService.IsOnline)
				{
					this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)];
					return;
				}
				if (this.PhoneNumber.Length < 4)
				{
					this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.PhoneValidationLength)];
					return;
				}
				this.codeSent = true;
				ServiceRef.LogService.LogInformational("Phone validation code simulated as sent.");
				if (this.CoordinatorViewModel is not null)
					this.CoordinatorViewModel.GoToNextCommand.Execute(null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)];
			}
			finally
			{
				this.IsBusy = false;
			}
		}
	}
}
