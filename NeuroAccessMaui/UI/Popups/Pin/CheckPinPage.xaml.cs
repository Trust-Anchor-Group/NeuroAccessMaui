using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Localization;
using System.Globalization;

namespace NeuroAccessMaui.UI.Popups.Pin
{
	public partial class CheckPinPage
	{
		private TaskCompletionSource<string?> result = new();

		public CheckPinPage(ImageSource? Background = null) : base(Background)
		{
			this.InitializeComponent();
			this.BindingContext = this;

			this.CheckPin(App.SelectedLanguage.Name);
		}

		public Task<string?> Result => this.result.Task;

		[RelayCommand]
		public void CheckPin(object Option)
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

			if ((SelectedLanguage is not null) && (Name != CultureInfo.CurrentUICulture.Name))
			{
				Preferences.Set("user_selected_language", SelectedLanguage.TwoLetterISOLanguageName);
				LocalizationManager.Current.CurrentCulture = SelectedLanguage;
			}
		}
	}
}
