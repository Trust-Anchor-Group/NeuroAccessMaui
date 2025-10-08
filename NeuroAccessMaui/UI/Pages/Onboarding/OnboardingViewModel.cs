using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private readonly List<OnboardingStep> stepOrder = new()
		{
			OnboardingStep.Welcome,
			OnboardingStep.AccountSetup,
			OnboardingStep.PinSetup,
			OnboardingStep.BaseIdApplication,
			OnboardingStep.Summary
		};

		private readonly Dictionary<OnboardingStep, BaseOnboardingStepViewModel> stepViewModels = new();
		private readonly OnboardingNavigationArgs? navigationArgs;
		private bool isUpdatingSelection;

		public OnboardingViewModel()
			: this(null)
		{
		}

		public OnboardingViewModel(OnboardingNavigationArgs? args)
		{
			this.navigationArgs = args;
			this.CurrentStep = this.stepOrder[0];
			this.selectedStateKey = this.CurrentStep.ToStateKey();
			this.HeaderTitle = this.GetStepTitle(this.CurrentStep);
		}

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
		private ObservableCollection<LanguageInfo> languages = new(App.SupportedLanguages);

		[ObservableProperty]
		private LanguageInfo selectedLanguage = App.SelectedLanguage;

		public bool IsSummaryStep => this.CurrentStep == OnboardingStep.Summary;

		public bool IsOnWelcomeStep => this.CurrentStep == OnboardingStep.Welcome;

		public void RegisterStep(BaseOnboardingStepViewModel stepViewModel)
		{
			if (!this.stepViewModels.ContainsKey(stepViewModel.Step))
			{
				this.stepViewModels[stepViewModel.Step] = stepViewModel;
				stepViewModel.AttachCoordinator(this);
				if (stepViewModel.Step == this.CurrentStep)
				{
					this.HeaderTitle = this.GetStepTitle(stepViewModel.Step);
				}
			}
		}

		public T GetStepViewModel<T>(OnboardingStep step) where T : BaseOnboardingStepViewModel
		{
			if (this.stepViewModels.TryGetValue(step, out BaseOnboardingStepViewModel viewModel) && viewModel is T typed)
				return typed;

			throw new InvalidOperationException($"Step '{step}' has not been registered.");
		}

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			OnboardingStep target = this.navigationArgs?.InitialStep ?? this.stepOrder[0];
			await this.MoveToStepAsync(target).ConfigureAwait(false);
		}

		[RelayCommand]
		private async Task GoToNext()
		{
			if (!this.stepViewModels.TryGetValue(this.CurrentStep, out BaseOnboardingStepViewModel current))
				return;

			if (!await current.OnNextAsync().ConfigureAwait(false))
				return;

			int index = this.stepOrder.IndexOf(this.CurrentStep);
			if (index < 0)
				return;

			if (index >= this.stepOrder.Count - 1)
			{
				await this.CompleteOnboardingAsync().ConfigureAwait(false);
				return;
			}

			OnboardingStep nextStep = this.stepOrder[index + 1];
			await this.MoveToStepAsync(nextStep).ConfigureAwait(false);
		}

		public override async Task GoBack()
		{
			int index = this.stepOrder.IndexOf(this.CurrentStep);
			if (index <= 0)
				return;

			if (this.stepViewModels.TryGetValue(this.CurrentStep, out BaseOnboardingStepViewModel current))
			{
				await current.OnBackAsync().ConfigureAwait(false);
			}

			OnboardingStep previous = this.stepOrder[index - 1];
			await this.MoveToStepAsync(previous).ConfigureAwait(false);
		}

		[RelayCommand]
		private async Task GoToStep(OnboardingStep step)
		{
			await this.MoveToStepAsync(step).ConfigureAwait(false);
		}

		private async Task MoveToStepAsync(OnboardingStep step)
		{
			if (!this.stepViewModels.ContainsKey(step))
				return;

			this.CurrentStep = step;
			try
			{
				this.isUpdatingSelection = true;
				this.SelectedStateKey = step.ToStateKey();
			}
			finally
			{
				this.isUpdatingSelection = false;
			}

			this.HeaderTitle = this.GetStepTitle(step);
			this.CanGoBack = this.stepOrder.IndexOf(step) > 0;

			if (this.stepViewModels.TryGetValue(step, out BaseOnboardingStepViewModel vm))
			{
				await vm.OnActivatedAsync().ConfigureAwait(false);
			}
		}

		private string GetStepTitle(OnboardingStep step)
		{
			if (this.stepViewModels.TryGetValue(step, out BaseOnboardingStepViewModel vm) && !string.IsNullOrWhiteSpace(vm.Title))
				return vm.Title;

			return step.ToString();
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
			await ServiceRef.UiService.DisplayAlert(
				AppResources.OnboardingContactSupportTitle,
				AppResources.OnboardingContactSupportDetailsPart1,
				AppResources.Ok);
		}

		private async Task CompleteOnboardingAsync()
		{
			await Task.CompletedTask;
		}

		partial void OnSelectedStateKeyChanged(string value)
		{
			if (this.isUpdatingSelection)
				return;

			if (Enum.TryParse(value, out OnboardingStep step) && step != this.CurrentStep)
			{
				_ = this.MoveToStepAsync(step);
			}
		}

		partial void OnSelectedLanguageChanged(LanguageInfo value)
		{
			if (value is null)
				return;

			if (!Equals(App.SelectedLanguage, value))
			{
				//TODO
				//App.SelectedLanguage = value;
			}
		}
	}
}
