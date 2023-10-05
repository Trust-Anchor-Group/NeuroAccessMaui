using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Popups;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

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
			this.registrationSteps.Add(this.AddChildViewModel((BaseRegistrationViewModel)Item.BindingContext));
		};

		this.CurrentStep = (int)ServiceRef.TagProfile.Step;
	}


	//!!!
	/*
	/// <summary>
	/// See <see cref="CurrentStep"/>
	/// </summary>
	public static readonly BindableProperty CurrentStepProperty =
		BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(RegistrationViewModel), -1, propertyChanged: OnCurrentStepPropertyChanged);

	static void OnCurrentStepPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((RegistrationViewModel)bindable).OnCurrentStepPropertyChanged();

	async void OnCurrentStepPropertyChanged()
	{
		this.UpdateStepVariables();

		await this.RegistrationSteps[this.CurrentStep].DoAssignProperties();
	}
	*/

	/// <summary>
	/// Gets or sets the current step from the list of <see cref="registrationSteps"/>.
	/// </summary>
	[ObservableProperty]
	int currentStep;

	/// <summary>
	/// The list of steps needed to register a digital identity.
	/// </summary>
	private readonly Collection<BaseRegistrationViewModel> registrationSteps = new();

	[ObservableProperty]
	private ObservableCollection<LanguageInfo> languages = new(App.SupportedLanguages);

	[ObservableProperty]
	private LanguageInfo selectedLanguage = App.SelectedLanguage;

	public void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
	{
		this.SelectedLanguage = App.SelectedLanguage;
	}

	[RelayCommand]
	private async Task ChangeLanguage()
	{
		//await MopupService.Instance.PushAsync(ServiceHelper.GetService<SelectLanguagePage>());
		await MopupService.Instance.PushAsync(new SelectLanguagePage());

		//LocalizationManager.Current.CurrentCulture = language;
	}
}
