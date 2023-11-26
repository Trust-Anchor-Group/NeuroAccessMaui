using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Popups;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using CommunityToolkit.Mvvm.Messaging;

namespace NeuroAccessMaui.UI.Pages.Registration;

public partial class RegistrationViewModel : BaseViewModel
{
	[ObservableProperty]
	private string currentState = "Loading";

	public RegistrationViewModel()
	{

	}

	/// <inheritdoc/>
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();

		LocalizationManager.Current.PropertyChanged += this.PropertyChangedEventHandler;
	}

	/// <inheritdoc/>
	protected override async Task OnDispose()
	{
		LocalizationManager.Current.PropertyChanged -= this.PropertyChangedEventHandler;

		await base.OnDispose();
	}

	/// <summary>
	/// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
	/// </summary>
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

	public void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
	{
		this.SelectedLanguage = App.SelectedLanguage;
	}

	public bool CanGoToPrev => (ServiceRef.TagProfile.Step > RegistrationStep.RequestPurpose)
		// The PIN definition isn't really a part of the registration prosess, so disable the back button.
		&& (ServiceRef.TagProfile.Step < RegistrationStep.DefinePin);

	/// <summary>
	/// The command to bind to for moving backwards to the previous step in the registration process.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanGoToPrev))]
	private async Task GoToPrev()
	{
		try
		{
			switch (this.CurrentStep)
			{
				case RegistrationStep.CreateAccount:
					//!!! this.registrationSteps[this.CurrentStep].ClearStepState();
					ServiceRef.TagProfile.ClearAccount();
					ServiceRef.TagProfile.GoToStep(RegistrationStep.ChooseProvider);
					break;

				case RegistrationStep.ChooseProvider:
					//!!! this.registrationSteps[this.CurrentStep].ClearStepState();
					ServiceRef.TagProfile.GoToStep(RegistrationStep.ValidateEmail);
					break;

				case RegistrationStep.ValidateEmail:
					//!!! this.registrationSteps[this.CurrentStep].ClearStepState();
					ServiceRef.TagProfile.GoToStep(RegistrationStep.ValidatePhone);
					break;

				case RegistrationStep.ValidatePhone:
					//!!! this.registrationSteps[this.CurrentStep].ClearStepState();
					ServiceRef.TagProfile.GoToStep(RegistrationStep.RequestPurpose);
					break;

				default: // something forgotten?
					throw new NotImplementedException();
			}

			//!!! await this.SyncTagProfileStep();

			WeakReferenceMessenger.Default.Send(new RegistrationPageMessage(ServiceRef.TagProfile.Step));
		}
		catch (Exception ex)
		{
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
	}

	[RelayCommand]
	private async Task ChangeLanguage()
	{
		SelectLanguagePage Page = new();
		await MopupService.Instance.PushAsync(Page);
	}
}
