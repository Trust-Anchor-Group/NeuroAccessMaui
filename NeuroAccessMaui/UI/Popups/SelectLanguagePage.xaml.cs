using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Popups;

public partial class SelectLanguagePage
{
	/// <summary>
	/// Popup factory
	/// </summary>
	public static async Task<SelectLanguagePage> Create()
	{
		SelectLanguageViewModel ViewModel = new();
		return new SelectLanguagePage(ViewModel, null);
/*
		try
		{
			Stream ScreenStream = await ServiceRef.PlatformSpecific.CaptureScreen(10);
			ImageSource Background = ImageSource.FromStream(() => ScreenStream);

			return new SelectLanguagePage(ViewModel, Background);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);

			return new SelectLanguagePage(ViewModel, null);
		}
*/
	}

	private SelectLanguageViewModel viewModel;

	public double ViewWidth => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);
	public double ViewHeight => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (1.0 / 2.0);

	private SelectLanguagePage(SelectLanguageViewModel ViewModel, ImageSource? Background) : base(Background)
	{
		this.InitializeComponent();
		this.BindingContext = this;
		this.viewModel = ViewModel;

		this.InnerListView.SetBinding(CollectionView.ItemsSourceProperty,
			new Binding(nameof(ViewModel.ItemsSource), source: ViewModel));

		this.InnerListView.SetBinding(CollectionView.SelectedItemProperty,
			new Binding(nameof(ViewModel.SelectedItem), source: ViewModel, mode: BindingMode.TwoWay));

		if (Application.Current?.Resources.TryGetValue("SelectLanguageDataTemplate", out object DataTemplate) ?? false)
		{
			this.InnerListView.ItemTemplate = (DataTemplate)DataTemplate;
		}
	}

	private void InnerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
	}
}
