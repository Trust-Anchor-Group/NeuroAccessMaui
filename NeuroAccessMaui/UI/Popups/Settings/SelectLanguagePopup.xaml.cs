using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using System.Globalization;
namespace NeuroAccessMaui.UI.Popups.Settings
{
	public partial class SelectLanguagePopup
	{
		public override double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);
		public List<LanguageInfo> Languages { get; } = new(App.SupportedLanguages);

		public SelectLanguagePopup()
		{
			this.InitializeComponent();

			this.BindingContext = this;
			this.SelectLanguage(App.SelectedLanguage.Name);

		}

		[RelayCommand]
		public void SelectLanguage(object Option)
		{
			if (Option is not string Name)
				return;

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
						VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Normal);
				}
			}

			if ((SelectedLanguage is not null) && (Name != CultureInfo.CurrentCulture.Name))
			{
				Preferences.Set("user_selected_language", SelectedLanguage.TwoLetterISOLanguageName);
				LocalizationManager.Current.CurrentCulture = SelectedLanguage;
			}
			ServiceRef.UiService.PopAsync();
		}
	}
}
