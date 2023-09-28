using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.Pages.Registration;

public partial class RegistrationViewModel : BaseViewModel
{
	[ObservableProperty]
	private string currentState = "Loading";

	public RegistrationViewModel()
	{

	}

	[ObservableProperty]
	private ObservableCollection<LanguageInfo> languages = new(App.SupportedLanguages);

	[ObservableProperty]
	private LanguageInfo selectedLanguage = App.SelectedLanguage;

	[RelayCommand]
	private async Task ChangeLanguage()
	{
			 //LocalizationManager.Current.CurrentCulture = language;
		await Task.CompletedTask;
	}
}
