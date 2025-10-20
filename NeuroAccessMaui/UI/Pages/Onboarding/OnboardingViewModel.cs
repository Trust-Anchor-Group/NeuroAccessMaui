using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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
		private List<OnboardingStep> activeSequence = new List<OnboardingStep>();
		private readonly OnboardingNavigationArgs navigationArgs;
		private readonly OnboardingScenario scenario;
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
			OnboardingNavigationArgs? Args = ServiceRef.UiService.PopLatestArgs<OnboardingNavigationArgs>();
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

		public override async Task GoBack()
		{
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

		[RelayCommand]
		private async Task GoToStep(OnboardingStep Step)
		{
			NavigationDirection Direction = this.DetermineDirection(this.CurrentStep, Step);
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

		private bool TryGetSkipReason(OnboardingStep Step, out string Reason)
		{
			ITagProfile Profile = ServiceRef.TagProfile;
			bool HasEstablishedIdentity = this.HasEstablishedIdentity(Profile);
			switch (Step)
			{
				case OnboardingStep.Welcome:
					// Always show Welcome now.
					break;
				case OnboardingStep.ValidatePhone:
					// Always redo phone verification; never skip.
					break;
				case OnboardingStep.ValidateEmail:
					// Always redo email verification; never skip.
					break;
			case OnboardingStep.CreateAccount:
					if (Profile.LegalIdentity is LegalIdentity Identity &&
						(Identity.State == IdentityState.Approved || Identity.State == IdentityState.Created))
					{
						Reason = "IdentityAlreadyPresent";
						return true;
					}
					break;
			case OnboardingStep.NameEntry:
					if (!string.IsNullOrEmpty(Profile.Account))
					{
						Reason = "AccountAlreadySelected";
						return true;
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
			this.CanGoBack = this.FindStepInDirection(TargetStep, NavigationDirection.Backward).HasValue;

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
	}
}
