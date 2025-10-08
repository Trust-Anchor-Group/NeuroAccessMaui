using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public abstract partial class BaseOnboardingStepViewModel : BaseViewModel
	{
		private static readonly AsyncRelayCommand disabledAsyncCommand = new(async () => await Task.CompletedTask);

		protected BaseOnboardingStepViewModel(OnboardingStep step)
		{
			this.Step = step;
		}

	internal void AttachCoordinator(OnboardingViewModel coordinator)
	{
		this.Coordinator = coordinator;
		this.OnPropertyChanged(nameof(this.NextCommand));
		this.OnPropertyChanged(nameof(this.BackCommand));
		this.OnPropertyChanged(nameof(this.GoToStepCommand));
		this.OnPropertyChanged(nameof(this.CoordinatorViewModel));
	}

		public OnboardingStep Step { get; }

	protected OnboardingViewModel Coordinator { get; private set; } = null!;

	public OnboardingViewModel? CoordinatorViewModel => this.Coordinator;

		public IAsyncRelayCommand NextCommand => this.Coordinator?.GoToNextCommand ?? disabledAsyncCommand;

		public IAsyncRelayCommand BackCommand => this.Coordinator?.GoBackCommand ?? disabledAsyncCommand;

		public IAsyncRelayCommand<OnboardingStep> GoToStepCommand => this.Coordinator?.GoToStepCommand ?? new AsyncRelayCommand<OnboardingStep>(_ => Task.CompletedTask);

		public virtual string Title => this.Step.ToString();

		public virtual string Description => string.Empty;

	public virtual string NextButtonText => "Continue";

	public virtual string BackButtonText => "Back";

		internal virtual Task<bool> OnNextAsync()
		{
			return Task.FromResult(true);
		}

		internal virtual Task OnActivatedAsync()
		{
			return Task.CompletedTask;
		}

		internal virtual Task OnBackAsync()
		{
			return Task.CompletedTask;
		}
	}
}
