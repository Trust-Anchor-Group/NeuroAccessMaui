using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Localization;
using System.Globalization;

namespace NeuroAccessMaui.UI.Popups
{
	public partial class SelectLanguagePopup
	{
		/*
		/// <summary>
		/// Popup factory
		/// </summary>
		public static Task<SelectLanguagePopup> Create()
		{
			//SelectLanguageViewModel ViewModel = ServiceHelper.GetService<SelectLanguageViewModel>();
			//return Task.FromResult(new SelectLanguagePopup(ViewModel));

			return await MainThread.InvokeOnMainThreadAsync(async () => await
			{
				SelectLanguageViewModel ViewModel = new();

				try
				{
					byte[] ScreenBitmap = await ServiceRef.PlatformSpecific.CaptureScreen(10);

					ImageSource Background = ImageSource.FromStream(() => new MemoryStream(ScreenBitmap));
					Page = new SelectLanguagePopup(ViewModel, Background);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					await ServiceRef.UiSerializer.DisplayException(ex);

					Page = new SelectLanguagePopup(ViewModel, null);
				}

				return Page;
			});
		}
		*/
		public override double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

		public List<LanguageInfo> Languages { get; } = new(App.SupportedLanguages);

		public SelectLanguagePopup(ImageSource? Background = null)
			: base(Background)
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
		}
	}
}
