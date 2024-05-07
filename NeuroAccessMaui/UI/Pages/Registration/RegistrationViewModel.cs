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
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;
			ServiceRef.TagProfile.StepChanged += this.TagProfile_StepChanged;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;
			ServiceRef.TagProfile.StepChanged -= this.TagProfile_StepChanged;

			await base.OnDispose();
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
		[NotifyCanExecuteChangedFor(nameof(GoToPrevCommand))]
		RegistrationStep currentStep = RegistrationStep.Complete;

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
				return (ServiceRef.TagProfile.Step > RegistrationStep.ValidatePhone) &&
					// Disable the back button after the accpunt was created
					string.IsNullOrEmpty(ServiceRef.TagProfile?.Account ?? string.Empty)
					&& (ServiceRef.TagProfile?.Step < RegistrationStep.DefinePassword);
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
					case RegistrationStep.CreateAccount:
						ServiceRef.TagProfile.ClearAccount();
						NewStep = RegistrationStep.ChooseProvider;
						break;

					case RegistrationStep.ChooseProvider:
						NewStep = RegistrationStep.ValidateEmail;
						break;

					case RegistrationStep.ValidateEmail:
						NewStep = RegistrationStep.ValidatePhone;
						break;

					case RegistrationStep.ValidatePhone:
						NewStep = RegistrationStep.RequestPurpose;
						break;

					default: // Should not happen. Something forgotten? 
						throw new NotImplementedException();
				}

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
			await ServiceRef.UiService.PushAsync<SelectLanguagePopup>();
		}
	}
}
