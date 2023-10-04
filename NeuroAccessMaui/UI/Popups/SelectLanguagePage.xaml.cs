using System.Collections.ObjectModel;
using System.Globalization;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.Popups;

public partial class SelectLanguagePage
{
	/// <summary>
	/// Popup factory
	/// </summary>
	//public static Task<SelectLanguagePage> Create()
	//{
		//SelectLanguageViewModel ViewModel = ServiceHelper.GetService<SelectLanguageViewModel>();
		//return Task.FromResult(new SelectLanguagePage(ViewModel));

		/*
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
		})
		*/
	//}

	//private SelectLanguageViewModel viewModel;

	public double ViewWidth => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);
	public double ViewHeight => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (1.0 / 2.0);

	public Collection<LanguageInfo> ItemsSource { get; set; } = new(App.SupportedLanguages);
	public LanguageInfo SelectedItem { get; set; } = App.SelectedLanguage;

	public SelectLanguagePage(ImageSource? Background = null) : base(Background)
	{
		this.InitializeComponent();
		this.BindingContext = this;
	}

	private void InnerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (this.SelectedItem.IsCurrent)
		{
			return;
		}

		Task ExecutionTask = this.Dispatcher.DispatchAsync(() => this.InnerListView.ScrollTo(this.SelectedItem));

		Preferences.Set("user_selected_language", this.SelectedItem.TwoLetterISOLanguageName);
		LocalizationManager.Current.CurrentCulture = this.SelectedItem;
	}
}
