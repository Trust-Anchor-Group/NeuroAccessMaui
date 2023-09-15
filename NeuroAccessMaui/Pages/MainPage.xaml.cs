using NeuroAccessMaui.Pages;

namespace NeuroAccessMaui;

public partial class MainPage
{
	public MainPage(MainViewModel ViewModel)
	{
		this.InitializeComponent();
		this.BindingContext = ViewModel;
	}
}
