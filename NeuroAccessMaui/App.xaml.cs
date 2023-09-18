using NeuroAccessMaui.Services;

namespace NeuroAccessMaui;

public partial class App : Application
{
	public App()
	{
		this.InitializeComponent();

		this.MainPage = ServiceHelper.GetService<Pages.AppShell>();
	}
}
