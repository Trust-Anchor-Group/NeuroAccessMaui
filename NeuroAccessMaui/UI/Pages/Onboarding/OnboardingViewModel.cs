using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui;
using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.UI.Popups.Settings;

namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	public partial class OnboardingViewModel : BaseViewModel
	{
		private readonly Dictionary<OnboardingStep, BaseOnboardingStepViewModel> stepViewModels = new();
		private readonly OnboardingNavigationArgs? navigationArgs;
		private bool isUpdatingSelection;
		private List<OnboardingStep> activeSequence = new();

		public OnboardingViewModel() : this(null) { }
		public OnboardingViewModel(OnboardingNavigationArgs? Args) { this.navigationArgs = Args; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsSummaryStep))]
		[NotifyPropertyChangedFor(nameof(IsOnWelcomeStep))]
		private OnboardingStep currentStep;

		[ObservableProperty] private string selectedStateKey = string.Empty;
		[ObservableProperty][NotifyCanExecuteChangedFor(nameof(GoBackCommand))] private bool canGoBack;
		[ObservableProperty] private string headerTitle = string.Empty;
		[ObservableProperty] private ObservableCollection<LanguageInfo> languages = new(App.SupportedLanguages);
		[ObservableProperty] private LanguageInfo selectedLanguage = App.SelectedLanguage;

		// Finalize is the last step in current onboarding sequence.
		public bool IsSummaryStep => this.CurrentStep == OnboardingStep.Finalize;
		public bool IsOnWelcomeStep => this.CurrentStep == OnboardingStep.Welcome;

		public void RegisterStep(BaseOnboardingStepViewModel StepViewModel)
		{
			if (!this.stepViewModels.ContainsKey(StepViewModel.Step))
			{
				this.stepViewModels[StepViewModel.Step] = StepViewModel;
				StepViewModel.AttachCoordinator(this);
				if (StepViewModel.Step == this.CurrentStep)
					this.HeaderTitle = this.GetStepTitle(StepViewModel.Step);
			}
		}

		public T GetStepViewModel<T>(OnboardingStep Step) where T : BaseOnboardingStepViewModel
		{
			if (this.stepViewModels.TryGetValue(Step, out BaseOnboardingStepViewModel StepVm) && StepVm is T Typed)
				return Typed;
			throw new InvalidOperationException($"Step '{Step}' has not been registered.");
		}

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			this.BuildActiveSequence();
			OnboardingStep Target = this.GetInitialStep();
			await this.MoveToStepAsync(Target).ConfigureAwait(false);
		}

		private void BuildActiveSequence()
		{
			OnboardingScenario Scenario = this.navigationArgs?.Scenario ?? OnboardingScenario.FullSetup;
			this.activeSequence = Scenario switch
			{
				OnboardingScenario.FullSetup => new List<OnboardingStep>
				{
					OnboardingStep.Welcome,
					OnboardingStep.ValidatePhone,
					OnboardingStep.ValidateEmail,
					OnboardingStep.CreateAccount,
					OnboardingStep.DefinePassword,
					OnboardingStep.Biometrics,
					OnboardingStep.Finalize
				},
				OnboardingScenario.ChangePin => new List<OnboardingStep>
				{
					OnboardingStep.DefinePassword,
					OnboardingStep.Biometrics,
					OnboardingStep.Finalize
				},
				OnboardingScenario.ReverifyIdentity => new List<OnboardingStep>
				{
					OnboardingStep.ValidatePhone,
					OnboardingStep.ValidateEmail,
					OnboardingStep.CreateAccount,
					OnboardingStep.Finalize
				},
				_ => new List<OnboardingStep> { OnboardingStep.Welcome }
			};
		}

		private OnboardingStep GetInitialStep()
		{
			OnboardingStep? Requested = this.navigationArgs?.InitialStep;
			if (Requested.HasValue && this.activeSequence.Contains(Requested.Value))
				return Requested.Value;
			return this.activeSequence.FirstOrDefault();
		}

		[RelayCommand]
		private async Task GoToNext()
		{
			if (this.stepViewModels.TryGetValue(this.CurrentStep, out BaseOnboardingStepViewModel Current))
			{
				if (!await Current.OnNextAsync().ConfigureAwait(false))
					return;
			}

			int Index = this.activeSequence.IndexOf(this.CurrentStep);
			if (Index < 0)
				Index = 0;
			if (Index >= this.activeSequence.Count - 1)
			{
				await this.CompleteOnboardingAsync().ConfigureAwait(false);
				return;
			}
			OnboardingStep Next = this.activeSequence[Index + 1];
			await this.MoveToStepAsync(Next).ConfigureAwait(false);
		}

		public override async Task GoBack()
		{
			int Index = this.activeSequence.IndexOf(this.CurrentStep);
			if (Index <= 0)
				return;
			if (this.stepViewModels.TryGetValue(this.CurrentStep, out BaseOnboardingStepViewModel Current))
				await Current.OnBackAsync().ConfigureAwait(false);
			OnboardingStep Previous = this.activeSequence[Index - 1];
			await this.MoveToStepAsync(Previous).ConfigureAwait(false);
		}

		[RelayCommand]
		private async Task GoToStep(OnboardingStep Step) => await this.MoveToStepAsync(Step).ConfigureAwait(false);

		private async Task MoveToStepAsync(OnboardingStep Step)
		{
			this.CurrentStep = Step;
			try { this.isUpdatingSelection = true; this.SelectedStateKey = Step.ToStateKey(); }
			finally { this.isUpdatingSelection = false; }
			this.HeaderTitle = this.GetStepTitle(Step);
			this.CanGoBack = this.activeSequence.IndexOf(Step) > 0;
			if (this.stepViewModels.TryGetValue(Step, out BaseOnboardingStepViewModel Vm))
				await Vm.OnActivatedAsync().ConfigureAwait(false);
		}

		private string GetStepTitle(OnboardingStep Step)
		{
			if (this.stepViewModels.TryGetValue(Step, out BaseOnboardingStepViewModel Vm) && !string.IsNullOrWhiteSpace(Vm.Title))
				return Vm.Title;
			return Step.ToString();
		}

		[RelayCommand]
		private async Task ChangeLanguage()
		{
			await ServiceRef.PopupService.PushAsync<SelectLanguagePopup>();
			this.SelectedLanguage = App.SelectedLanguage;
		}

		[RelayCommand]
		private async Task ExistingAccount()
		{
			await this.MoveToStepAsync(OnboardingStep.ContactSupport);
		}

		private async Task CompleteOnboardingAsync() => await Task.CompletedTask;

		partial void OnSelectedStateKeyChanged(string Value)
		{
			if (this.isUpdatingSelection)
				return;
			if (Enum.TryParse(Value, out OnboardingStep ParsedStep) && ParsedStep != this.CurrentStep && this.activeSequence.Contains(ParsedStep))
				_ = this.MoveToStepAsync(ParsedStep);
		}

		partial void OnSelectedLanguageChanged(LanguageInfo Value)
		{
			if (Value is null)
				return;
			if (!Equals(App.SelectedLanguage, Value)) { }
		}
	}
}
