namespace NeuroAccessMaui.Pages.Main;

public partial class MainPage
{
	public MainPage(MainViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentPageModel = ViewModel;
	}
}
