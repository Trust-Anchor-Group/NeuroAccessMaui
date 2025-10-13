using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// ViewModel for validating e-mail in onboarding.
	/// </summary>
	public partial class ValidateEmailOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private bool codeSent;
		private static readonly Regex EmailRegex = new Regex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.IgnoreCase);

		public ValidateEmailOnboardingStepViewModel() : base(OnboardingStep.ValidateEmail) { }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSend))]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private string email = string.Empty;

		[ObservableProperty]
		private bool isBusy;

		[ObservableProperty]
		private string errorMessage = string.Empty;

		public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);
		public bool CanSend => !this.IsBusy && EmailRegex.IsMatch(this.Email);
		public bool CanContinue => this.codeSent;

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingEmailPageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingEmailPageDetails)];

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
				this.codeSent = true;
				ServiceRef.LogService.LogInformational("Email validation code simulated as sent.");
				if (this.CoordinatorViewModel is not null)
					this.CoordinatorViewModel.GoToNextCommand.Execute(null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingEmailCode)];
			}
			finally
			{
				this.IsBusy = false;
			}
		}
	}
}
