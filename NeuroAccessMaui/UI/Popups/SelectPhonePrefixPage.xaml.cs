using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Data.Countries;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.UI.Popups;

public partial class SelectPhonePrefixPage
{
	public override double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

	public List<ISO3166Country> Countries { get; } = new(ISO_3166_1.Countries);

	public SelectPhonePrefixPage(ImageSource? Background = null) : base(Background)
	{
		this.InitializeComponent();
		this.BindingContext = this;

		//!!! this.SelectPhonePrefix(App.SelectedLanguage.Name);
	}

	[RelayCommand]
	public void SelectPhonePrefix(object o)
	{
		if (o is not string Prefix)
		{
			return;
		}

		/*
		LanguageInfo? SelectedLanguage = null;

		foreach (object Item in this.LanguagesContainer)
		{
			if ((Item is VisualElement Element) &&
				(Element.BindingContext is LanguageInfo LanguageInfo))
			{
				if (Name == LanguageInfo.Name)
				{
					VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Selected);
					SelectedLanguage = LanguageInfo;

					Task ExecutionTask = this.Dispatcher.DispatchAsync(() => this.InnerScrollView.ScrollToAsync(Element, ScrollToPosition.MakeVisible, true));
				}
				else
				{
					VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Normal);
				}
			}
		}

		if ((SelectedLanguage is not null) && (Name != CultureInfo.CurrentUICulture.Name))
		{

			Preferences.Set("user_selected_language", SelectedLanguage.TwoLetterISOLanguageName);
			LocalizationManager.Current.CurrentCulture = SelectedLanguage;
		}
		*/
	}
}
