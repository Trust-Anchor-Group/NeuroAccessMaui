using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// ViewModel for creating an account in the onboarding flow.
	/// </summary>
	public partial class CreateAccountOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public CreateAccountOnboardingStepViewModel() : base(OnboardingStep.CreateAccount) { }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private string userName = string.Empty;

		[ObservableProperty]
		private string errorMessage = string.Empty;

		public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);
		public bool CanContinue => !string.IsNullOrWhiteSpace(this.UserName) && this.UserName.Length >= 3;

		// Updated to existing resource keys.
		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingAccountPageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingAccountPageDetails)];

		internal override Task<bool> OnNextAsync()
		{
			if (!this.CanContinue)
			{
				this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.UsernameNameAlreadyTaken)];
				ServiceRef.LogService.LogWarning("Cannot advance: username too short or invalid.");
				return Task.FromResult(false);
			}
			ServiceRef.LogService.LogInformational("Username accepted. Advancing to DefinePassword step.");
			return Task.FromResult(true);
		}
	}
}
