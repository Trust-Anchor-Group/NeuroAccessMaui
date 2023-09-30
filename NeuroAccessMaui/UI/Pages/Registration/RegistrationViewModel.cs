using System.Collections.ObjectModel;
using System.Globalization;
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

	[ObservableProperty]
	private ObservableCollection<LanguageInfo> languages = new(App.SupportedLanguages);

	[ObservableProperty]
	private LanguageInfo selectedLanguage = App.SelectedLanguage;

	[RelayCommand]
	private async Task ChangeLanguage()
	{
		await MopupService.Instance.PushAsync(ServiceHelper.GetService<SelectLanguagePage>());

		//LocalizationManager.Current.CurrentCulture = language;
	}
}
