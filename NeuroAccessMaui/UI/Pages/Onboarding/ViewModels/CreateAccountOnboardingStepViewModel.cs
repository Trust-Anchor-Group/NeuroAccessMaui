using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Registration;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Onboarding step for creating (applying for) a legal identity. Username uniqueness is assumed handled server side.
	/// </summary>
	public partial class CreateAccountOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private bool hasAppliedForIdentity;
		private bool callbacksRegistered;

		public CreateAccountOnboardingStepViewModel() : base(OnboardingStep.CreateAccount) { }

		#region Observable

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private string userName = string.Empty;

		[ObservableProperty]
		private string errorMessage = string.Empty;

		[ObservableProperty]
		private bool isBusy;

		#endregion

		/// <summary>
		/// True if an error message exists.
		/// </summary>
		public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);

		/// <summary>
		/// Show Continue when username entered &amp; identity created or application in progress.
		/// </summary>
		public bool CanContinue => !string.IsNullOrWhiteSpace(this.UserName) && this.UserName.Length >= 3 && this.IsLegalIdentityCreated;

		/// <summary>
		/// Title.
		/// </summary>
		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingAccountPageTitle)];

		/// <summary>
		/// Description.
		/// </summary>
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingAccountPageDetails)];

		/// <summary>
		/// If XMPP connected.
		/// </summary>
		public bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;
		/// <summary>
		/// If account created (TagProfile already has account credentials set by earlier steps).
		/// </summary>
		public bool IsAccountCreated => !string.IsNullOrEmpty(ServiceRef.TagProfile.Account);
		/// <summary>
		/// If a legal identity exists (Approved or Created).
		/// </summary>
		public bool IsLegalIdentityCreated => ServiceRef.TagProfile.LegalIdentity is not null &&
			(ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved || ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Created);

		/// <summary>
		/// Can issue CreateIdentity command.
		/// </summary>
		public bool CanCreateIdentity => this.IsAccountCreated && !this.IsLegalIdentityCreated && !this.IsBusy;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			if (!this.callbacksRegistered)
			{
				ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
				ServiceRef.XmppService.LegalIdentityChanged += this.XmppContracts_LegalIdentityChanged;
				this.callbacksRegistered = true;
			}
			await ServiceRef.XmppService.WaitForConnectedState(TimeSpan.FromSeconds(10));
			await this.TryAutoApplyAsync();
		}

		/// <inheritdoc/>
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

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			if (NewState == XmppState.Connected)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await this.TryAutoApplyAsync();
				});
			}
			return Task.CompletedTask;
		}

		private Task XmppContracts_LegalIdentityChanged(object _, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.IsLegalIdentityCreated));
				this.OnPropertyChanged(nameof(this.CanContinue));
				if (this.IsLegalIdentityCreated && this.CoordinatorViewModel is not null)
				{
					this.CoordinatorViewModel.GoToNextCommand.Execute(null);
				}
			});
			return Task.CompletedTask;
		}

		private async Task TryAutoApplyAsync()
		{
			if (!this.hasAppliedForIdentity && this.CanCreateIdentity)
			{
				await this.CreateIdentityCommand.ExecuteAsync(null);
			}
		}

		/// <summary>
		/// Command to create/apply for legal identity.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanCreateIdentity))]
		private async Task CreateIdentity()
		{
			if (this.hasAppliedForIdentity)
				return;

			this.hasAppliedForIdentity = true;
			this.ErrorMessage = string.Empty;
			this.IsBusy = true;

			using CancellationTokenSource timerCts = new(TimeSpan.FromSeconds(Constants.Timeouts.GenericRequest.Seconds));
			bool appliedSuccessfully = false;
			LegalIdentity? Identity = null;

			while (!timerCts.Token.IsCancellationRequested)
			{
				try
				{
					RegisterIdentityModel identityModel = this.CreateRegisterModel();
					LegalIdentityAttachment[] photos = []; // none in onboarding
					(bool succeeded, LegalIdentity? addedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
						ServiceRef.XmppService.AddLegalIdentity(identityModel, true, photos));
					if (succeeded && addedIdentity is not null)
					{
						Identity = addedIdentity;
						await ServiceRef.TagProfile.SetLegalIdentity(Identity, true);
						appliedSuccessfully = true;
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
			{
				this.ErrorMessage = ServiceRef.Localizer[nameof(AppResources.PleaseTryAgain)];
				await ServiceRef.TagProfile.ClearLegalIdentity();
				ServiceRef.LogService.LogWarning("Legal identity application failed in onboarding.");
			}

			this.IsBusy = false;
			this.OnPropertyChanged(nameof(this.IsLegalIdentityCreated));
			this.OnPropertyChanged(nameof(this.CanContinue));
			if (appliedSuccessfully && this.CoordinatorViewModel is not null)
			{
				this.CoordinatorViewModel.GoToNextCommand.Execute(null);
			}
		}

		private RegisterIdentityModel CreateRegisterModel()
		{
			RegisterIdentityModel identityModel = new();
			string Value;
			if (!string.IsNullOrWhiteSpace(Value = ServiceRef.TagProfile?.PhoneNumber?.Trim() ?? string.Empty))
				identityModel.PhoneNr = Value;
			if (!string.IsNullOrWhiteSpace(Value = ServiceRef.TagProfile?.EMail?.Trim() ?? string.Empty))
				identityModel.EMail = Value;
			if (!string.IsNullOrWhiteSpace(Value = ServiceRef.TagProfile?.SelectedCountry?.Trim() ?? string.Empty))
				identityModel.CountryCode = Value;
			return identityModel;
		}

		internal override Task<bool> OnNextAsync()
		{
			// Coordinator will call this when Next pressed. Allow only if identity created.
			return Task.FromResult(this.IsLegalIdentityCreated);
		}
	}
}
