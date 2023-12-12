using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Localization;
using System.Globalization;

namespace NeuroAccessMaui.UI.Popups
{
	public partial class SelectLanguagePage
	{
		/*
		/// <summary>
		/// Popup factory
		/// </summary>
		public static Task<SelectLanguagePage> Create()
		{
			//SelectLanguageViewModel ViewModel = ServiceHelper.GetService<SelectLanguageViewModel>();
			//return Task.FromResult(new SelectLanguagePage(ViewModel));

			return await MainThread.InvokeOnMainThreadAsync(async () => await
			{
				SelectLanguageViewModel ViewModel = new();

				try
				{
					byte[] ScreenBitmap = await ServiceRef.PlatformSpecific.CaptureScreen(10);

					ImageSource Background = ImageSource.FromStream(() => new MemoryStream(ScreenBitmap));
					Page = new SelectLanguagePage(ViewModel, Background);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					await ServiceRef.UiSerializer.DisplayException(ex);

					Page = new SelectLanguagePage(ViewModel, null);
				}

				return Page;
			});
		}
		*/
		public override double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

		public List<LanguageInfo> Languages { get; } = new(App.SupportedLanguages);

		public SelectLanguagePage(ImageSource? Background = null) : base(Background)
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

			if ((SelectedLanguage is not null) && (Name != CultureInfo.CurrentUICulture.Name))
			{
				Preferences.Set("user_selected_language", SelectedLanguage.TwoLetterISOLanguageName);
				LocalizationManager.Current.CurrentCulture = SelectedLanguage;
			}
		}
	}
}
