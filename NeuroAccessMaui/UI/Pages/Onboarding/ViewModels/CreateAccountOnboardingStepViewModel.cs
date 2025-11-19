using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Mirrors registration create-account behaviour for onboarding: waits for XMPP, applies for identity, and advances automatically.
	/// </summary>
	public partial class CreateAccountOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private bool hasAppliedForIdentity;
		private bool callbacksRegistered;

		public CreateAccountOnboardingStepViewModel() : base(OnboardingStep.CreateAccount) { }

		[ObservableProperty]
		private bool isBusy;

		[ObservableProperty]
		private string errorMessage = string.Empty;

		[ObservableProperty]
		private string statusMessage = string.Empty;

		public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);
		public bool HasStatusMessage => !string.IsNullOrEmpty(this.StatusMessage);
		public bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;
		public bool IsAccountCreated => !string.IsNullOrEmpty(ServiceRef.TagProfile.Account);
		public bool IsLegalIdentityCreated => ServiceRef.TagProfile.LegalIdentity is LegalIdentity identity && identity.State == IdentityState.Approved;
		public bool IsIdentityPendingApproval => ServiceRef.TagProfile.LegalIdentity is LegalIdentity identity && identity.State == IdentityState.Created;
		public bool IsWaitingForIdentity => this.IsBusy || this.IsIdentityPendingApproval || (this.hasAppliedForIdentity && !this.IsLegalIdentityCreated);
		public bool CanCreateIdentity => this.IsAccountCreated && ServiceRef.TagProfile.LegalIdentity is null && !this.IsBusy;

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (!this.callbacksRegistered)
			{
				ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
				ServiceRef.XmppService.LegalIdentityChanged += this.XmppContracts_LegalIdentityChanged;
				this.callbacksRegistered = true;
			}

			if (this.CoordinatorViewModel?.Scenario == OnboardingScenario.ReverifyIdentity &&
				ServiceRef.TagProfile.LegalIdentity is LegalIdentity existingIdentity &&
				existingIdentity.State == IdentityState.Approved)
			{
				ServiceRef.LogService.LogInformational("Clearing approved identity prior to reverify onboarding step.");
				await ServiceRef.TagProfile.ClearLegalIdentity();
			}

			await ServiceRef.XmppService.WaitForConnectedState(TimeSpan.FromSeconds(10));
			await this.TryAutoApplyAsync();
			await this.CheckApplicationStateAsync();
		}

		internal override async Task OnActivatedAsync()
		{
			await base.OnActivatedAsync();
			await this.TryAutoApplyAsync();
			await this.CheckApplicationStateAsync();
		}

		public override async Task OnDisposeAsync()
		{
			if (this.callbacksRegistered)
			{
				ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
				ServiceRef.XmppService.LegalIdentityChanged -= this.XmppContracts_LegalIdentityChanged;
				this.callbacksRegistered = false;
			}

			await base.OnDisposeAsync();
		}

		private Task XmppService_ConnectionStateChanged(object _, XmppState newState)
		{
			if (newState == XmppState.Connected)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await this.TryAutoApplyAsync();
					await this.CheckApplicationStateAsync();
				});
			}

			return Task.CompletedTask;
		}

		private Task XmppContracts_LegalIdentityChanged(object _, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				this.OnPropertyChanged(nameof(this.IsLegalIdentityCreated));
				this.OnPropertyChanged(nameof(this.IsIdentityPendingApproval));
				this.OnPropertyChanged(nameof(this.IsWaitingForIdentity));
				await this.CheckApplicationStateAsync();
			});

			return Task.CompletedTask;
		}

		private async Task TryAutoApplyAsync()
		{
			// Guard: Must have created account and active XMPP connection before applying for identity.
			if (!this.IsAccountCreated || ServiceRef.XmppService.State != XmppState.Connected)
				return;

			// Guard: Prevent duplicate attempts while already applied or busy.
			if (!this.CanCreateIdentity || this.hasAppliedForIdentity)
				return;

			// Ensure required XMPP services are discovered before attempting identity creation.
			if (ServiceRef.TagProfile.NeedsUpdating())
			{
				try
				{
					XmppClient? client = ServiceRef.XmppService.State == XmppState.Connected ? ServiceRef.XmppService.GetType().GetProperty("ContractsClient") is null ? null : null : null; // placeholder to avoid reflection; discovery uses default client
					await ServiceRef.XmppService.DiscoverServices();
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					return; // Abort until services available.
				}
			}

			this.SetHasAppliedForIdentity(true);
			this.ErrorMessage = string.Empty;
			this.StatusMessage = string.Empty;
			this.IsBusy = true;

			using CancellationTokenSource timerCts = new(TimeSpan.FromSeconds(Constants.Timeouts.GenericRequest.Seconds));
			bool appliedSuccessfully = false;
			LegalIdentity? identity = null;

			while (!timerCts.Token.IsCancellationRequested)
			{
				try
				{
					RegisterIdentityModel model = this.CreateRegisterModel();
					LegalIdentityAttachment[] photos = []; // No photos during onboarding
					(bool succeeded, LegalIdentity? addedIdentity) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.AddLegalIdentity(model, true, photos));
					if (succeeded && addedIdentity is not null)
					{
						identity = addedIdentity;
						await ServiceRef.TagProfile.SetLegalIdentity(identity, true);
						appliedSuccessfully = true;
						this.StatusMessage = ServiceRef.Localizer[nameof(AppResources.IdApplicationSentDescription)];
						break;
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}

				try
				{
					await Task.Delay(TimeSpan.FromSeconds(5), timerCts.Token);
				}
				catch (TaskCanceledException)
				{
					break;
				}
			}

			if (!appliedSuccessfully)
				this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.PleaseTryAgain)];

			this.IsBusy = false;
			this.OnPropertyChanged(nameof(this.IsLegalIdentityCreated));
			this.OnPropertyChanged(nameof(this.IsIdentityPendingApproval));
			this.OnPropertyChanged(nameof(this.IsWaitingForIdentity));

			if (!appliedSuccessfully)
			{
				await ServiceRef.TagProfile.ClearLegalIdentity();
				ServiceRef.LogService.LogWarning("Legal identity application failed during onboarding.");
				this.SetHasAppliedForIdentity(false); // Allow future retry.
			}
		}

		private async Task CheckApplicationStateAsync()
		{
			if (ServiceRef.TagProfile.LegalIdentity is not LegalIdentity legalIdentity)
			{
				if (!this.IsAccountCreated)
					return;
				if (!this.hasAppliedForIdentity)
					await this.TryAutoApplyAsync();
				return;
			}

			switch (legalIdentity.State)
			{
				case IdentityState.Approved:
					this.ErrorMessage = string.Empty;
					this.StatusMessage = string.Empty;
					if (this.CoordinatorViewModel is not null)
						await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.DefinePassword);
					break;
				case IdentityState.Created:
					this.ErrorMessage = string.Empty;
					this.StatusMessage = ServiceRef.Localizer[nameof(AppResources.IdApplicationSentDescription)];
					break;
				case IdentityState.Rejected:
				case IdentityState.Obsoleted:
				case IdentityState.Compromised:
					await this.HandleIdentityFailureAsync(legalIdentity.State);
					break;
				default:
					if (legalIdentity.IsDiscarded())
					{
						await this.HandleIdentityFailureAsync(legalIdentity.State);
					}
					break;
			}

			this.OnPropertyChanged(nameof(this.IsLegalIdentityCreated));
			this.OnPropertyChanged(nameof(this.IsIdentityPendingApproval));
			this.OnPropertyChanged(nameof(this.IsWaitingForIdentity));
		}

		private async Task HandleIdentityFailureAsync(IdentityState state)
		{
			this.StatusMessage = string.Empty;
			switch (state)
			{
				case IdentityState.Compromised:
					this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.YourLegalIdentityHasBeenCompromised)];
					break;
				case IdentityState.Obsoleted:
					this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.YourLegalIdentityHasBeenObsoleted)];
					break;
				default:
					this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.YourApplicationWasRejected)];
					break;
			}

			await ServiceRef.TagProfile.ClearLegalIdentity();
			this.SetHasAppliedForIdentity(false);
			this.OnPropertyChanged(nameof(this.IsLegalIdentityCreated));
			this.OnPropertyChanged(nameof(this.IsIdentityPendingApproval));

			if (this.CoordinatorViewModel is not null)
				await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.ValidatePhone);
		}

		private void SetHasAppliedForIdentity(bool value)
		{
			if (this.hasAppliedForIdentity == value)
				return;

			this.hasAppliedForIdentity = value;
			this.OnPropertyChanged(nameof(this.IsWaitingForIdentity));
		}

		private RegisterIdentityModel CreateRegisterModel()
		{
			RegisterIdentityModel model = new();
			string value;
			if (!string.IsNullOrWhiteSpace(value = ServiceRef.TagProfile?.PhoneNumber?.Trim() ?? string.Empty))
				model.PhoneNr = value;
			if (!string.IsNullOrWhiteSpace(value = ServiceRef.TagProfile?.EMail?.Trim() ?? string.Empty))
				model.EMail = value;
			if (!string.IsNullOrWhiteSpace(value = ServiceRef.TagProfile?.SelectedCountry?.Trim() ?? string.Empty))
				model.CountryCode = value;
			return model;
		}

		partial void OnStatusMessageChanged(string value)
		{
			this.OnPropertyChanged(nameof(this.HasStatusMessage));
		}

		partial void OnIsBusyChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.IsWaitingForIdentity));
		}
	}
}
