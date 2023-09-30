using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.Popups;

public partial class SelectLanguageViewModel : ObservableObject
{
	public SelectLanguageViewModel() { }

	[ObservableProperty]
	private Collection<LanguageInfo> itemsSource = new(App.SupportedLanguages);

	[ObservableProperty]
	private LanguageInfo selectedItem = App.SelectedLanguage;
}
