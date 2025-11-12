using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;
using NeuroAccessMaui.UI.Pages.Startup;
using NeuroAccessMaui.UI.Popups.Settings;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	public partial class OnboardingViewModel : BaseViewModel
	{
		private sealed record StepDescriptor(OnboardingStep Step, Func<bool> Include, Func<bool> Skip);

		private readonly Dictionary<OnboardingStep, BaseOnboardingStepViewModel> stepViewModels = new Dictionary<OnboardingStep, BaseOnboardingStepViewModel>();
		private readonly Dictionary<OnboardingStep, StepDescriptor> descriptorMap = new Dictionary<OnboardingStep, StepDescriptor>();
		private readonly List<StepDescriptor> descriptorOrder = new List<StepDescriptor>();
		private readonly List<OnboardingStep> scenarioSequence = new List<OnboardingStep>();
		private readonly HashSet<OnboardingStep> loggedSkips = new HashSet<OnboardingStep>();
		private readonly HashSet<OnboardingStep> completedSteps = new HashSet<OnboardingStep>();
		private List<OnboardingStep> activeSequence = new List<OnboardingStep>();
		private readonly OnboardingNavigationArgs navigationArgs;
		private readonly OnboardingScenario scenario;
		private OnboardingTransferContext? transferContext;
		private bool isUpdatingSelection;
		private bool isCompleting;
		private bool hasLoggedSequence;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsSummaryStep))]
		[NotifyPropertyChangedFor(nameof(IsOnWelcomeStep))]
		private OnboardingStep currentStep;

		[ObservableProperty]
		private string selectedStateKey = string.Empty;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
		private bool canGoBack;

		[ObservableProperty]
		private string headerTitle = string.Empty;

		[ObservableProperty]
		private ObservableCollection<LanguageInfo> languages = new ObservableCollection<LanguageInfo>(App.SupportedLanguages);

		[ObservableProperty]
		private LanguageInfo selectedLanguage = App.SelectedLanguage;

		public OnboardingViewModel()
		{
			OnboardingNavigationArgs? Args = ServiceRef.NavigationService.PopLatestArgs<OnboardingNavigationArgs>();
			if (Args is null)
			{
				ServiceRef.LogService.LogWarning("Onboarding navigation arguments missing. Using defaults.");
				Args = new OnboardingNavigationArgs();
			}

			this.navigationArgs = Args;
			this.scenario = this.navigationArgs.Scenario;
			ServiceRef.LogService.LogInformational("Onboarding scenario selected.",
				new KeyValuePair<string, object?>("Scenario", this.scenario.ToString()));
			this.InitializeDescriptors();
		}

		public bool IsSummaryStep => this.CurrentStep == OnboardingStep.Finalize;

		public bool IsOnWelcomeStep => this.CurrentStep == OnboardingStep.Welcome;

		public OnboardingScenario Scenario => this.scenario;

		internal bool HasPendingTransfer => this.transferContext is not null;

		internal bool PendingTransferIncludesLegalIdentity => this.transferContext?.HasLegalIdentity == true;

		public void RegisterStep(BaseOnboardingStepViewModel stepViewModel)
		{
			if (this.stepViewModels.ContainsKey(stepViewModel.Step))
			{
				return;
			}

			this.stepViewModels[stepViewModel.Step] = stepViewModel;
			stepViewModel.AttachCoordinator(this);
			if (stepViewModel.Step == this.CurrentStep)
			{
				this.HeaderTitle = this.GetStepTitle(stepViewModel.Step);
			}
		}

		internal Task ApplyTransferContextAsync(OnboardingTransferContext context)
		{
			this.transferContext = context;
			ServiceRef.LogService.LogInformational("Transfer context applied.",
				new KeyValuePair<string, object?>("HasLegalIdentity", context.HasLegalIdentity));
			this.BuildActiveSequence();
			return Task.CompletedTask;
		}

		internal async Task<bool> TryFinalizeTransferAsync(bool showBusyOverlay)
		{
			OnboardingTransferContext? context = this.transferContext;
			if (context is null)
			{
				return false;
			}

			if (showBusyOverlay)
			{
				await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(true));
			}

			try
			{
				bool connected = await ConnectToAccountAsync(
					context.AccountName,
					context.Password,
					context.PasswordMethod,
					string.Empty,
					context.LegalIdDefinition,
					string.Empty).ConfigureAwait(false);

				if (connected)
				{
					this.transferContext = null;
					this.BuildActiveSequence();
				}

				return connected;
			}
			finally
			{
				if (showBusyOverlay)
				{
					await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(false));
				}
			}
		}

        /// <summary>
        /// Attempts to connect to the transferred account and import any provided legal identity definition.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <param name="password">The password.</param>
        /// <param name="passwordMethod">The password hashing method.</param>
        /// <param name="legalIdentityJid">Optional legal identity JID to select, if provided.</param>
        /// <param name="legalIdDefinition">Optional legal identity definition to import.</param>
        /// <param name="pin">Optional local PIN to store.</param>
        /// <returns>true if the connection Succeeded; otherwise, false.</returns>
        internal static async Task<bool> ConnectToAccountAsync(string accountName, string password, string passwordMethod, string legalIdentityJid, XmlElement? legalIdDefinition, string pin)
        {
            ServiceRef.LogService.LogInformational("Attempting transferred account connection.",
                new KeyValuePair<string, object?>("Account", accountName),
                new KeyValuePair<string, object?>("Domain", ServiceRef.TagProfile.Domain));

            try
            {
                async Task OnConnected(XmppClient client)
                {
                    ServiceRef.LogService.LogInformational("XMPP account connected. Discovering identities.");
                    DateTime Now = DateTime.Now;
                    LegalIdentity? CreatedIdentity = null;
                    LegalIdentity? ApprovedIdentity = null;
                    bool ServiceDiscoverySucceeded = ServiceRef.TagProfile.NeedsUpdating() ? await ServiceRef.XmppService.DiscoverServices(client) : true;
                    ServiceRef.LogService.LogInformational($"Service discovery result: {ServiceDiscoverySucceeded}.");
                    if (ServiceDiscoverySucceeded && !string.IsNullOrEmpty(ServiceRef.TagProfile.LegalJid))
                    {
                        bool DestroyContractsClient = false;
                        if (!client.TryGetExtension(typeof(ContractsClient), out IXmppExtension Extension) || Extension is not ContractsClient contractsClient)
                        {
                            contractsClient = new ContractsClient(client, ServiceRef.TagProfile.LegalJid);
                            DestroyContractsClient = true;
                            ServiceRef.LogService.LogDebug("ContractsClient created for identity operations.");
                        }

                        try
                        {
                            if (legalIdDefinition is not null)
                            {
                                ServiceRef.LogService.LogInformational("Importing provided LegalId keys.");
                                await contractsClient.ImportKeys(legalIdDefinition);
                            }

                            LegalIdentity[] identities = await contractsClient.GetLegalIdentitiesAsync();
                            ServiceRef.LogService.LogInformational($"Fetched {identities.Length} identities.");
                            foreach (LegalIdentity identity in identities)
                            {
                                try
                                {
                                    if ((string.IsNullOrEmpty(legalIdentityJid) || string.Compare(legalIdentityJid, identity.Id, StringComparison.OrdinalIgnoreCase) == 0) &&
                                        identity.HasClientSignature && identity.HasClientPublicKey && identity.From <= Now && identity.To >= Now &&
                                        (identity.State == IdentityState.Approved || identity.State == IdentityState.Created) && identity.ValidateClientSignature() && await contractsClient.HasPrivateKey(identity))
                                    {
                                        if (identity.State == IdentityState.Approved)
                                        {
                                            ApprovedIdentity = identity;
                                            break;
                                        }

                                        CreatedIdentity ??= identity;
                                    }
                                }
                                catch (Exception ex2)
                                {
                                    ServiceRef.LogService.LogException(ex2);
                                }
                            }

                            LegalIdentity? SelectedIdentity = ApprovedIdentity ?? CreatedIdentity;
                            string SelectedId;
                            if (SelectedIdentity is not null)
                            {
                                ServiceRef.LogService.LogInformational($"Selected identity '{SelectedIdentity.Id}' (State={SelectedIdentity.State}).");
                                await ServiceRef.TagProfile.SetAccountAndLegalIdentity(accountName, client.PasswordHash, client.PasswordHashMethod, SelectedIdentity);
                                SelectedId = SelectedIdentity.Id;
                                ServiceRef.TagProfile.SetXmppPasswordNeedsUpdating(true);
                            }
                            else
                            {
                                ServiceRef.LogService.LogInformational("No identity selected; storing account only.");
                                ServiceRef.TagProfile.SetAccount(accountName, client.PasswordHash, client.PasswordHashMethod);
                                SelectedId = string.Empty;
                            }

                            if (!string.IsNullOrEmpty(pin))
                            {
                                ServiceRef.LogService.LogInformational("Local PIN set from invitation.");
                                ServiceRef.TagProfile.LocalPassword = pin;
                            }

                            foreach (LegalIdentity identity in identities)
                            {
                                if (identity.Id == SelectedId)
                                    continue;

                                switch (identity.State)
                                {
                                    case IdentityState.Approved:
                                    case IdentityState.Created:
                                        ServiceRef.LogService.LogDebug($"Obsoleting identity '{identity.Id}'.");
                                        await contractsClient.ObsoleteLegalIdentityAsync(identity.Id);
                                        break;
                                }
                            }
                        }
                        finally
                        {
                            if (DestroyContractsClient)
                            {
                                ServiceRef.LogService.LogDebug("Disposing temporary ContractsClient.");
                                contractsClient.Dispose();
                            }
                        }
                    }
                }

                (string HostName, int PortNumber, bool IsIp) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(ServiceRef.TagProfile.Domain ?? string.Empty);
                ServiceRef.LogService.LogInformational($"Connecting to XMPP account Host='{HostName}' Port={PortNumber} IsIp={IsIp}.");
                (bool Succeeded, string? ErrorMessage, string[]? _) = await ServiceRef.XmppService.TryConnectAndConnectToAccount(
                    ServiceRef.TagProfile.Domain ?? string.Empty,
                    IsIp,
                    HostName,
                    PortNumber,
                    accountName,
                    password,
                    passwordMethod,
                    Constants.LanguageCodes.Default,
                    typeof(App).Assembly,
                    OnConnected);
                ServiceRef.LogService.LogInformational($"XMPP connect result: {(Succeeded ? "Succeeded" : "Failed")} Error='{ErrorMessage}'.");
                if (!Succeeded && !string.IsNullOrEmpty(ErrorMessage))
                    ServiceRef.LogService.LogWarning(ErrorMessage);
                return Succeeded;
            }
            catch (Exception ex)
            {
                ServiceRef.LogService.LogException(ex);
            }

            return false;
        }

		internal OnboardingStep? GetNextActiveStep(OnboardingStep step)
		{
			this.BuildActiveSequence();
			return this.FindStepInDirection(step, NavigationDirection.Forward);
		}

		internal void MarkStepCompleted(OnboardingStep step)
		{
			if (!this.completedSteps.Add(step))
			{
				return;
			}

			ServiceRef.LogService.LogInformational("Onboarding step marked as completed.",
				new KeyValuePair<string, object?>("Step", step.ToString()),
				new KeyValuePair<string, object?>("Scenario", this.scenario.ToString()));

			this.BuildActiveSequence();
		}

		public T GetStepViewModel<T>(OnboardingStep step) where T : BaseOnboardingStepViewModel
		{
			if (this.stepViewModels.TryGetValue(step, out BaseOnboardingStepViewModel? stepViewModel) && stepViewModel is T typed)
			{
				return typed;
			}

			throw new InvalidOperationException($"Step '{step}' has not been registered.");
		}

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync().ConfigureAwait(false);

			this.BuildActiveSequence();
			if (this.activeSequence.Count == 0)
			{
				this.activeSequence.Add(OnboardingStep.Finalize);
			}

			if (this.activeSequence.Count == 1 && this.activeSequence[0] == OnboardingStep.Finalize)
			{
				await this.MoveToStepAsync(OnboardingStep.Finalize, NavigationDirection.Forward).ConfigureAwait(false);
				await this.CompleteOnboardingAsync(false, true).ConfigureAwait(false);
				return;
			}

			OnboardingStep StartStep = this.ResolveInitialStep();
			await this.MoveToStepAsync(StartStep, NavigationDirection.Forward).ConfigureAwait(false);
		}

		/// <summary>
		/// Advances the onboarding process to the next step, completing onboarding if no further steps are available.
		/// </summary>
		/// <remarks>If the current step does not allow advancement, the method does not proceed to the next step.
		/// When the final step is reached, onboarding is completed automatically.</remarks>
		/// <returns>A task that represents the asynchronous operation of moving to the next onboarding step or completing onboarding.</returns>
		[RelayCommand]
		private async Task GoToNext()
		{
			if (this.stepViewModels.TryGetValue(this.CurrentStep, out BaseOnboardingStepViewModel? CurrentStepViewModel))
			{
				bool CanAdvance = await CurrentStepViewModel.OnNextAsync().ConfigureAwait(false);
				if (!CanAdvance)
				{
					return;
				}
			}

			this.BuildActiveSequence();
			OnboardingStep? NextStep = this.FindStepInDirection(this.CurrentStep, NavigationDirection.Forward);
			if (NextStep is null)
			{
				await this.CompleteOnboardingAsync().ConfigureAwait(false);
				return;
			}

			await this.MoveToStepAsync(NextStep.Value, NavigationDirection.Forward).ConfigureAwait(false);
		}

		/// <summary>
		/// Navigates to the previous onboarding step if backward navigation is permitted from the current step.
		/// </summary>
		/// <remarks>If the current step restricts backward navigation, this method does not perform any navigation.
		/// If there is no previous step available, the method completes without changing the current step.</remarks>
		/// <returns>A task that represents the asynchronous navigation operation. The task completes when the navigation to the
		/// previous step has finished, or immediately if backward navigation is restricted or no previous step exists.</returns>
		public override async Task GoBack()
		{
			if (IsBackRestrictedStep(this.CurrentStep))
			{
				return;
			}

			if (this.stepViewModels.TryGetValue(this.CurrentStep, out BaseOnboardingStepViewModel? CurrentStepViewModel))
			{
				await CurrentStepViewModel.OnBackAsync().ConfigureAwait(false);
			}

			this.BuildActiveSequence();
			OnboardingStep? PreviousStep = this.FindStepInDirection(this.CurrentStep, NavigationDirection.Backward);
			if (PreviousStep is null)
			{
				return;
			}

			await this.MoveToStepAsync(PreviousStep.Value, NavigationDirection.Backward).ConfigureAwait(false);
		}

		/// <summary>
		/// Navigates to the specified onboarding step asynchronously, updating the current step and direction as appropriate.
		/// </summary>
		/// <remarks>If navigation to a previous step is restricted, the operation will not proceed. This method
		/// should be called when transitioning between steps in the onboarding workflow.</remarks>
		/// <param name="Step">The onboarding step to navigate to. Must be a valid step within the onboarding process.</param>
		/// <returns>A task that represents the asynchronous navigation operation.</returns>
		[RelayCommand]
		private async Task GoToStep(OnboardingStep Step)
		{
			NavigationDirection Direction = this.DetermineDirection(this.CurrentStep, Step);
			if (Direction == NavigationDirection.Backward && IsBackRestrictedStep(this.CurrentStep))
			{
				return;
			}

			await this.MoveToStepAsync(Step, Direction).ConfigureAwait(false);
		}

		[RelayCommand]
		private async Task ChangeLanguage()
		{
			await ServiceRef.PopupService.PushAsync<SelectLanguagePopup>().ConfigureAwait(false);
			this.SelectedLanguage = App.SelectedLanguage;
		}

		[RelayCommand]
		private async Task ExistingAccount()
		{
			NavigationDirection Direction = this.DetermineDirection(this.CurrentStep, OnboardingStep.ContactSupport);
			await this.MoveToStepAsync(OnboardingStep.ContactSupport, Direction).ConfigureAwait(false);
		}

		/// <summary>
		/// Initializes the onboarding step descriptors and prepares the internal collections for the current onboarding
		/// scenario.
		/// </summary>
		/// <remarks>This method resets and populates the descriptor and sequence collections to reflect the steps
		/// required for the current scenario. It should be called before accessing onboarding step descriptors to ensure that
		/// all relevant steps and their conditions are up to date. This method is intended for internal use and is not
		/// thread-safe.</remarks>
		private void InitializeDescriptors()
		{
			this.descriptorOrder.Clear();
			this.descriptorMap.Clear();
			this.scenarioSequence.Clear();

			List<OnboardingStep> BaseSequence = this.GetBaseSequence(this.scenario);
			foreach (OnboardingStep Step in BaseSequence)
			{
				this.scenarioSequence.Add(Step);
			}

			HashSet<OnboardingStep> ScenarioSet = new HashSet<OnboardingStep>(this.scenarioSequence);

			this.AddDescriptor(OnboardingStep.Welcome,
				() => ScenarioSet.Contains(OnboardingStep.Welcome),
				() => this.TryGetSkipReason(OnboardingStep.Welcome, out _));
			this.AddDescriptor(OnboardingStep.ValidatePhone,
				() => ScenarioSet.Contains(OnboardingStep.ValidatePhone),
				() => this.TryGetSkipReason(OnboardingStep.ValidatePhone, out _));
			this.AddDescriptor(OnboardingStep.ValidateEmail,
				() => ScenarioSet.Contains(OnboardingStep.ValidateEmail),
				() => this.TryGetSkipReason(OnboardingStep.ValidateEmail, out _));
			this.AddDescriptor(OnboardingStep.NameEntry,
				() => ScenarioSet.Contains(OnboardingStep.NameEntry),
				() => this.TryGetSkipReason(OnboardingStep.NameEntry, out _));
			this.AddDescriptor(OnboardingStep.CreateAccount,
				() => ScenarioSet.Contains(OnboardingStep.CreateAccount),
				() => this.TryGetSkipReason(OnboardingStep.CreateAccount, out _));
			this.AddDescriptor(OnboardingStep.DefinePassword,
				() => ScenarioSet.Contains(OnboardingStep.DefinePassword),
				() => this.TryGetSkipReason(OnboardingStep.DefinePassword, out _));
			this.AddDescriptor(OnboardingStep.Biometrics,
				() => ScenarioSet.Contains(OnboardingStep.Biometrics),
				() => this.TryGetSkipReason(OnboardingStep.Biometrics, out _));
			this.AddDescriptor(OnboardingStep.Finalize,
				() => ScenarioSet.Contains(OnboardingStep.Finalize),
				() => false);
			this.AddDescriptor(OnboardingStep.ContactSupport,
				() => false,
				() => false);
		}

		private void AddDescriptor(OnboardingStep step, Func<bool> include, Func<bool> skip)
		{
			StepDescriptor descriptor = new StepDescriptor(step, include, skip);
			this.descriptorOrder.Add(descriptor);
			this.descriptorMap[step] = descriptor;
		}

		/// <summary>
		/// Builds the current sequence of active onboarding steps based on the configured descriptors and their inclusion or
		/// skip status.
		/// </summary>
		/// <remarks>This method updates the active onboarding sequence to reflect the latest configuration and
		/// conditions. If no steps are included, the sequence will contain only the finalization step. The method also logs
		/// any steps that are skipped, along with their reasons, and records changes to the sequence for auditing
		/// purposes.</remarks>
		private void BuildActiveSequence()
		{
			List<OnboardingStep> PreviousSequence = this.activeSequence;
			List<OnboardingStep> NewSequence = new List<OnboardingStep>();

			foreach (StepDescriptor Descriptor in this.descriptorOrder)
			{
				if (!Descriptor.Include())
				{
					continue;
				}

				OnboardingStep Step = Descriptor.Step;
				if (Descriptor.Skip() && Step != OnboardingStep.Finalize)
				{
					if (this.TryGetSkipReason(Step, out string Reason) && !string.IsNullOrEmpty(Reason) && !this.loggedSkips.Contains(Step))
					{
						this.loggedSkips.Add(Step);
						ServiceRef.LogService.LogInformational("Onboarding step skipped.",
							new KeyValuePair<string, object?>("Step", Step.ToString()),
							new KeyValuePair<string, object?>("Reason", Reason));
					}

					continue;
				}

				NewSequence.Add(Step);
			}

			if (NewSequence.Count == 0)
			{
				NewSequence.Add(OnboardingStep.Finalize);
			}

			this.activeSequence = NewSequence;

			bool SequencesEqual = this.AreSequencesEqual(PreviousSequence, NewSequence);
			if (!this.hasLoggedSequence || !SequencesEqual)
			{
				this.hasLoggedSequence = true;
				this.LogSequence("Built onboarding sequence.");
			}
		}

		/// <summary>
		/// Generates the default sequence of onboarding steps for the specified onboarding scenario.
		/// </summary>
		/// <param name="onboardingScenario">The onboarding scenario for which to retrieve the base sequence of steps. Determines the set and order of steps
		/// included in the returned list.</param>
		/// <returns>A list of <see cref="OnboardingStep"/> values representing the base sequence for the given scenario. The list
		/// reflects the recommended order of steps for the specified onboarding process.</returns>
		private List<OnboardingStep> GetBaseSequence(OnboardingScenario onboardingScenario)
		{
			List<OnboardingStep> Sequence = new List<OnboardingStep>();
			switch (onboardingScenario)
			{
				case OnboardingScenario.FullSetup:
					Sequence.Add(OnboardingStep.Welcome);
					Sequence.Add(OnboardingStep.ValidatePhone);
					Sequence.Add(OnboardingStep.ValidateEmail);
					Sequence.Add(OnboardingStep.NameEntry);
					Sequence.Add(OnboardingStep.CreateAccount);
					Sequence.Add(OnboardingStep.DefinePassword);
					Sequence.Add(OnboardingStep.Biometrics);
					Sequence.Add(OnboardingStep.Finalize);
					break;
				case OnboardingScenario.ChangePin:
					Sequence.Add(OnboardingStep.DefinePassword);
					Sequence.Add(OnboardingStep.Biometrics);
					Sequence.Add(OnboardingStep.Finalize);
					break;
				case OnboardingScenario.ReverifyIdentity:
					Sequence.Add(OnboardingStep.ValidatePhone);
					Sequence.Add(OnboardingStep.ValidateEmail);
					Sequence.Add(OnboardingStep.NameEntry);
					Sequence.Add(OnboardingStep.CreateAccount);
					Sequence.Add(OnboardingStep.Finalize);
					break;
				default:
					Sequence.Add(OnboardingStep.Welcome);
					Sequence.Add(OnboardingStep.Finalize);
					break;
			}

			return Sequence;
		}

		/// <summary>
		/// Determines whether the specified onboarding step should be skipped and provides the reason if it is skipped.
		/// </summary>
		/// <remarks>The skip reason is provided as a string value in <paramref name="Reason"/> when the method
		/// returns <see langword="true"/>. If the step is not skipped, <paramref name="Reason"/> is set to an empty string.
		/// The possible reasons include completed steps, existing identity or account, previously defined password, enabled
		/// biometrics, or unavailable biometric support.</remarks>
		/// <param name="Step">The onboarding step to evaluate for skipping.</param>
		/// <param name="Reason">When this method returns <see langword="true"/>, contains a string describing the reason the step is skipped;
		/// otherwise, set to an empty string.</param>
		/// <returns>true if the step should be skipped; otherwise, false.</returns>
		private bool TryGetSkipReason(OnboardingStep Step, out string Reason)
		{
			ITagProfile Profile = ServiceRef.TagProfile;
			bool HasEstablishedIdentity = this.HasEstablishedIdentity(Profile);
			if (this.completedSteps.Contains(Step))
			{
				Reason = "StepCompleted";
				return true;
			}

			switch (Step)
			{
				case OnboardingStep.Welcome:
					// Always show Welcome Now.
					break;
				case OnboardingStep.ValidatePhone:
					if (this.scenario != OnboardingScenario.ReverifyIdentity &&
						(this.transferContext?.HasLegalIdentity == true || HasEstablishedIdentity))
					{
						Reason = "IdentityAlreadyEstablished";
						return true;
					}
					break;
				case OnboardingStep.ValidateEmail:
					if (this.scenario != OnboardingScenario.ReverifyIdentity &&
						(this.transferContext?.HasLegalIdentity == true || HasEstablishedIdentity))
					{
						Reason = "IdentityAlreadyEstablished";
						return true;
					}
					break;
				case OnboardingStep.NameEntry:
					if (this.scenario != OnboardingScenario.ReverifyIdentity &&
						(!string.IsNullOrEmpty(Profile.Account) || this.transferContext is not null))
					{
						Reason = this.transferContext is not null ? "AccountTransferred" : "AccountAlreadySelected";
						return true;
					}
					break;
				case OnboardingStep.CreateAccount:
					if (this.scenario != OnboardingScenario.ReverifyIdentity)
					{
						bool HasTransferIdentity = this.transferContext?.HasLegalIdentity == true;
						bool HasApprovedIdentity = Profile.LegalIdentity is LegalIdentity identity && identity.State == IdentityState.Approved;
						if (HasTransferIdentity || HasApprovedIdentity)
						{
							Reason = HasTransferIdentity ? "AccountTransferred" : "IdentityAlreadyPresent";
							return true;
						}
					}
					break;
				case OnboardingStep.DefinePassword:
					if (this.scenario != OnboardingScenario.ChangePin && Profile.HasLocalPassword && !HasEstablishedIdentity)
					{
						Reason = "PasswordAlreadyDefined";
						return true;
					}
					break;
				case OnboardingStep.Biometrics:
					if (Profile.AuthenticationMethod == AuthenticationMethod.Fingerprint)
					{
						Reason = "BiometricsAlreadyEnabled";
						return true;
					}
					if (!ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication)
					{
						Reason = "BiometricsUnavailable";
						return true;
					}
					break;
			}

			Reason = string.Empty;
			return false;
		}

		private bool HasEstablishedIdentity(ITagProfile Profile)
		{
			if (Profile.LegalIdentity is LegalIdentity Identity)
			{
				return Identity.State == IdentityState.Approved || Identity.State == IdentityState.Created;
			}

			return false;
		}

		private OnboardingStep ResolveInitialStep()
		{
			ITagProfile Profile = ServiceRef.TagProfile;
			bool HasIdentity = this.HasEstablishedIdentity(Profile);
			bool HasAccount = !string.IsNullOrEmpty(Profile.Account);
			switch (this.scenario)
			{
				case OnboardingScenario.FullSetup:
					if (HasIdentity)
						return OnboardingStep.DefinePassword;
					if (HasAccount && !HasIdentity)
					{
						ServiceRef.LogService.LogInformational("Resuming FullSetup with existing account. Restarting 2FA.",
							new KeyValuePair<string, object?>("Account", Profile.Account));
						return OnboardingStep.ValidatePhone;
					}
					return OnboardingStep.Welcome;
				case OnboardingScenario.ReverifyIdentity:
					return OnboardingStep.ValidatePhone;
				case OnboardingScenario.ChangePin:
					return OnboardingStep.DefinePassword;
				default:
					return this.activeSequence.Count > 0 ? this.activeSequence[0] : OnboardingStep.Finalize;
			}
		}

		private async Task MoveToStepAsync(OnboardingStep Step, NavigationDirection Direction)
		{
			this.BuildActiveSequence();

			OnboardingStep TargetStep = this.ResolveStepWithSkipping(Step, Direction);
			this.CurrentStep = TargetStep;

			try
			{
				this.isUpdatingSelection = true;
				this.SelectedStateKey = TargetStep.ToStateKey();
			}
			finally
			{
				this.isUpdatingSelection = false;
			}

			this.HeaderTitle = this.GetStepTitle(TargetStep);
			this.CanGoBack = !IsBackRestrictedStep(TargetStep) && this.FindStepInDirection(TargetStep, NavigationDirection.Backward).HasValue;

			if (this.stepViewModels.TryGetValue(TargetStep, out BaseOnboardingStepViewModel? StepViewModel))
			{
				await StepViewModel.OnActivatedAsync().ConfigureAwait(false);
			}
		}

		private OnboardingStep ResolveStepWithSkipping(OnboardingStep Step, NavigationDirection Direction)
		{
			if (Step == OnboardingStep.ContactSupport)
			{
				return Step;
			}

			NavigationDirection EffectiveDirection = Direction == NavigationDirection.None ? NavigationDirection.Forward : Direction;
			OnboardingStep TargetStep = Step;
			int Guard = this.descriptorOrder.Count;

			while (Guard > 0)
			{
				if (!this.descriptorMap.TryGetValue(TargetStep, out StepDescriptor? Descriptor))
				{
					return TargetStep;
				}

				bool Include = Descriptor.Include();
				bool Skip = Descriptor.Skip();

				if (Include && (!Skip || TargetStep == OnboardingStep.Finalize))
				{
					return TargetStep;
				}

				if (Skip && this.TryGetSkipReason(TargetStep, out string Reason) && !string.IsNullOrEmpty(Reason) && !this.loggedSkips.Contains(TargetStep))
				{
					this.loggedSkips.Add(TargetStep);
					ServiceRef.LogService.LogInformational("Runtime skipped onboarding step.",
						new KeyValuePair<string, object?>("Step", TargetStep.ToString()),
						new KeyValuePair<string, object?>("Reason", Reason));
				}

				OnboardingStep? NextStep = this.FindStepInDirection(TargetStep, EffectiveDirection);
				if (!NextStep.HasValue)
				{
					break;
				}

				TargetStep = NextStep.Value;
				Guard--;
			}

			return TargetStep;
		}

		private OnboardingStep? FindStepInDirection(OnboardingStep ReferenceStep, NavigationDirection Direction)
		{
			NavigationDirection EffectiveDirection = Direction == NavigationDirection.None ? NavigationDirection.Forward : Direction;

			if (this.activeSequence.Count == 0)
			{
				return null;
			}

			int ActiveIndex = this.activeSequence.IndexOf(ReferenceStep);
			if (ActiveIndex >= 0)
			{
				if (EffectiveDirection == NavigationDirection.Forward)
				{
					if (ActiveIndex >= this.activeSequence.Count - 1)
					{
						return null;
					}

					return this.activeSequence[ActiveIndex + 1];
				}

				if (ActiveIndex == 0)
				{
					return null;
				}

				return this.activeSequence[ActiveIndex - 1];
			}

			int CanonicalIndex = this.GetCanonicalIndex(ReferenceStep);
			if (CanonicalIndex < 0)
			{
				return null;
			}

			if (EffectiveDirection == NavigationDirection.Forward)
			{
				for (int i = CanonicalIndex + 1; i < this.descriptorOrder.Count; i++)
				{
					StepDescriptor Descriptor = this.descriptorOrder[i];
					if (!Descriptor.Include())
					{
						continue;
					}

					if (Descriptor.Skip() && Descriptor.Step != OnboardingStep.Finalize)
					{
						continue;
					}

					if (this.activeSequence.Contains(Descriptor.Step))
					{
						return Descriptor.Step;
					}
				}
			}
			else
			{
				for (int i = CanonicalIndex - 1; i >= 0; i--)
				{
					StepDescriptor Descriptor = this.descriptorOrder[i];
					if (!Descriptor.Include())
					{
						continue;
					}

					if (Descriptor.Skip() && Descriptor.Step != OnboardingStep.Finalize)
					{
						continue;
					}

					if (this.activeSequence.Contains(Descriptor.Step))
					{
						return Descriptor.Step;
					}
				}
			}

			return null;
		}

		private int GetCanonicalIndex(OnboardingStep Step)
		{
			for (int i = 0; i < this.descriptorOrder.Count; i++)
			{
				if (this.descriptorOrder[i].Step == Step)
				{
					return i;
				}
			}

			return -1;
		}

		private NavigationDirection DetermineDirection(OnboardingStep OriginStep, OnboardingStep TargetStep)
		{
			if (OriginStep == TargetStep)
			{
				return NavigationDirection.None;
			}

			int OriginIndex = this.scenarioSequence.IndexOf(OriginStep);
			int TargetIndex = this.scenarioSequence.IndexOf(TargetStep);
			if (OriginIndex >= 0 && TargetIndex >= 0)
			{
				return TargetIndex >= OriginIndex ? NavigationDirection.Forward : NavigationDirection.Backward;
			}

			int CanonicalOriginIndex = this.GetCanonicalIndex(OriginStep);
			int CanonicalTargetIndex = this.GetCanonicalIndex(TargetStep);
			if (CanonicalOriginIndex >= 0 && CanonicalTargetIndex >= 0)
			{
				return CanonicalTargetIndex >= CanonicalOriginIndex ? NavigationDirection.Forward : NavigationDirection.Backward;
			}

			return NavigationDirection.None;
		}

		private string GetStepTitle(OnboardingStep Step)
		{
			if (this.stepViewModels.TryGetValue(Step, out BaseOnboardingStepViewModel? StepViewModel) && StepViewModel is not null && !string.IsNullOrWhiteSpace(StepViewModel.Title))
			{
				return StepViewModel.Title;
			}

			return Step.ToString();
		}

		private async Task CompleteOnboardingAsync(bool EnsureFinalizeVisible = true, bool AllStepsSkipped = false)
		{
			if (this.isCompleting)
			{
				return;
			}

			this.isCompleting = true;

			if (EnsureFinalizeVisible && this.CurrentStep != OnboardingStep.Finalize)
			{
				await this.MoveToStepAsync(OnboardingStep.Finalize, NavigationDirection.Forward).ConfigureAwait(false);
			}

			ServiceRef.LogService.LogInformational("Onboarding completion triggered.",
				new KeyValuePair<string, object?>("Scenario", this.scenario.ToString()),
				new KeyValuePair<string, object?>("Path", AllStepsSkipped ? "AllStepsSkipped" : "Standard"));

			await Task.Delay(TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
			await ServiceRef.NavigationService.SetRootAsync(nameof(LoadingPage)).ConfigureAwait(false);
		}

		private void LogSequence(string Message)
		{
			string StepsValue = string.Join(",", this.activeSequence);
			ServiceRef.LogService.LogInformational(Message,
				new KeyValuePair<string, object?>("Scenario", this.scenario.ToString()),
				new KeyValuePair<string, object?>("Steps", StepsValue));
		}

		private bool AreSequencesEqual(List<OnboardingStep> Left, List<OnboardingStep> Right)
		{
			if (ReferenceEquals(Left, Right))
			{
				return true;
			}

			if (Left.Count != Right.Count)
			{
				return false;
			}

			for (int i = 0; i < Left.Count; i++)
			{
				if (Left[i] != Right[i])
				{
					return false;
				}
			}

			return true;
		}

		partial void OnSelectedStateKeyChanged(string value)
		{
			if (this.isUpdatingSelection)
			{
				return;
			}

			if (Enum.TryParse(value, out OnboardingStep ParsedStep) && ParsedStep != this.CurrentStep && this.activeSequence.Contains(ParsedStep))
			{
				NavigationDirection Direction = this.DetermineDirection(this.CurrentStep, ParsedStep);
				if (Direction == NavigationDirection.Backward && IsBackRestrictedStep(this.CurrentStep))
				{
					return;
				}

				_ = this.MoveToStepAsync(ParsedStep, Direction);
			}
		}

		partial void OnSelectedLanguageChanged(LanguageInfo value)
		{
			if (value is null)
			{
				return;
			}

			if (!Equals(App.SelectedLanguage, value))
			{
				// Language synchronization handled elsewhere.
			}
		}

		private enum NavigationDirection
		{
			None = 0,
			Forward = 1,
			Backward = 2
		}

		private static bool IsBackRestrictedStep(OnboardingStep step)
		{
			return step == OnboardingStep.DefinePassword ||
				step == OnboardingStep.Biometrics ||
				step == OnboardingStep.Finalize;
		}
	}
}
