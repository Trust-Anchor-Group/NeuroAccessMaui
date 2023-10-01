namespace NeuroAccessMaui.Popups;

public partial class SelectLanguagePage
{
	private SelectLanguageViewModel viewModel;

	public double ViewWidth => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);
	public double ViewHeight => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (1.0 / 2.0);

	public SelectLanguagePage(SelectLanguageViewModel ViewModel)
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
