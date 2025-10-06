using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups.Settings;

namespace NeuroAccessMaui.UI.Pages.Registration
{
	public partial class RegistrationViewModel : BaseViewModel
	{
		public RegistrationViewModel()
		{
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;
			ServiceRef.TagProfile.StepChanged += this.TagProfile_StepChanged;

			await ServiceRef.IntentService.ProcessQueuedIntentsAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;
			ServiceRef.TagProfile.StepChanged -= this.TagProfile_StepChanged;

			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Adds sub-views
		/// </summary>
		/// <param name="Items">Views to add.</param>
		public void SetPagesContainer(List<BaseRegistrationView> Items)
		{
			foreach (BaseRegistrationView Item in Items)
			{
				BaseRegistrationViewModel ViewModel = (BaseRegistrationViewModel)Item.BindingContext;
				this.registrationSteps[ViewModel.Step] = this.AddChildViewModel(ViewModel);
			};
		}

		public Task DoAssignProperties(RegistrationStep Step)
		{
			this.CurrentStep = Step;
			return this.registrationSteps[Step].DoAssignProperties();
		}

		/// <summary>
		/// Gets or sets the current step from the list of <see cref="registrationSteps"/>.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanGoToPrev))]
		[NotifyPropertyChangedFor(nameof(IsOnGetStartedView))]
		[NotifyCanExecuteChangedFor(nameof(GoToPrevCommand))]
		RegistrationStep currentStep = RegistrationStep.Complete;

		public bool IsOnGetStartedView => this.CurrentStep == RegistrationStep.GetStarted;

		/// <summary>
		/// The list of steps needed to register a digital identity.
		/// </summary>
		private readonly SortedDictionary<RegistrationStep, BaseRegistrationViewModel> registrationSteps = [];

		[ObservableProperty]
		private ObservableCollection<LanguageInfo> languages = new(App.SupportedLanguages);

		[ObservableProperty]
		private LanguageInfo selectedLanguage = App.SelectedLanguage;

		public void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.SelectedLanguage = App.SelectedLanguage;
		}

		private void TagProfile_StepChanged(object? Sender, EventArgs e)
		{
			this.GoToPrevCommand.NotifyCanExecuteChanged();
		}

		public bool CanGoToPrev
		{
			get
			{
				switch (ServiceRef.TagProfile.Step)
				{

					case RegistrationStep.ValidatePhone:
					case RegistrationStep.ValidateEmail:
						return string.IsNullOrEmpty(ServiceRef.TagProfile.Account); // Disable the back button if account is already created
					case RegistrationStep.NameEntry:
					case RegistrationStep.ChooseProvider:
					case RegistrationStep.ContactSupport:
						return true;
					case RegistrationStep.GetStarted:
					case RegistrationStep.CreateAccount:
					case RegistrationStep.DefinePassword:
					case RegistrationStep.Complete:
					case RegistrationStep.Biometrics:
					case RegistrationStep.Finalize:
					default:
						return false;


				}
			}
		}

		/// <summary>
		/// The command to bind to for moving backwards to the previous step in the registration process.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanGoToPrev))]
		private async Task GoToPrev()
		{
			try
			{
				await this.registrationSteps[this.CurrentStep].DoClearProperties();

				RegistrationStep NewStep = ServiceRef.TagProfile.Step;
				switch (this.CurrentStep)
				{
					case RegistrationStep.NameEntry:
						NewStep = RegistrationStep.GetStarted;
						break;
					case RegistrationStep.ValidatePhone:
						NewStep = RegistrationStep.GetStarted;
						break;
					case RegistrationStep.ValidateEmail:
						NewStep = RegistrationStep.GetStarted;
						break;
					case RegistrationStep.ChooseProvider:
						NewStep = RegistrationStep.GetStarted;
						break;
					case RegistrationStep.ContactSupport:
						NewStep = RegistrationStep.GetStarted;
						break;

					default: // Should not happen. Something forgotten? 
						throw new NotImplementedException();
				}
				ServiceRef.PlatformSpecific.HideKeyboard();

				GoToRegistrationStep(NewStep);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand]
		private static async Task ChangeLanguage()
		{
			await ServiceRef.PopupService.PushAsync<SelectLanguagePopup>();
		}

		[RelayCommand]
		private void ExistingAccount()
		{
			GoToRegistrationStep(RegistrationStep.ContactSupport);
		}
	}
}
