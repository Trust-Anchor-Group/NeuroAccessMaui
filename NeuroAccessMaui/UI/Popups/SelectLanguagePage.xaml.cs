namespace NeuroAccessMaui.Popups;

public partial class SelectLanguagePage
{
	private SelectLanguageViewModel viewModel;

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

	private void PopupPage_BackgroundClicked(object sender, EventArgs e)
	{
	}

	private void InnerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
	}
}
