namespace NeuroAccessMaui.Pages;

public partial class MainPage
{
	public MainPage(MainViewModel ViewModel)
	{
		this.InitializeComponent();
		this.BindingContext = ViewModel;
	}
}
