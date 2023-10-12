using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.UI.Popups;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.Pages.Registration;

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
	private readonly SortedDictionary<RegistrationStep, BaseRegistrationViewModel> registrationSteps = new();

	[ObservableProperty]
	private ObservableCollection<LanguageInfo> languages = new(App.SupportedLanguages);

	[ObservableProperty]
	private LanguageInfo selectedLanguage = App.SelectedLanguage;

	public void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
	{
		this.SelectedLanguage = App.SelectedLanguage;
	}

	public bool CanGoToPrev => (ServiceRef.TagProfile.Step > RegistrationStep.RequestPurpose) && (ServiceRef.TagProfile.Step < RegistrationStep.Complete);

	/// <summary>
	/// The command to bind to for moving backwards to the previous step in the registration process.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanGoToPrev))]
	private async Task GoToPrev()
	{
		try
		{
			//!!!
			/*
				switch ((RegistrationStep)this.CurrentStep)
				{
					case RegistrationStep.Account:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearAccount();
						break;

					case RegistrationStep.RegisterIdentity:
						this.RegistrationSteps[(int)RegistrationStep.Account].ClearStepState();
						await this.TagProfile.ClearAccount(false);
						this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity].ClearStepState();
						await this.TagProfile.ClearLegalIdentity();
						await this.TagProfile.InvalidateContactInfo();
						break;

					case RegistrationStep.ValidateIdentity:
						RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
						vm.PopulateFromTagProfile();
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearIsValidated();
						break;

					case RegistrationStep.Pin:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.RevertPinStep();
						break;

					default: // RegistrationStep.Operator
						await this.TagProfile.ClearDomain();
						break;
				}

				await this.SyncTagProfileStep();
			*/
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
