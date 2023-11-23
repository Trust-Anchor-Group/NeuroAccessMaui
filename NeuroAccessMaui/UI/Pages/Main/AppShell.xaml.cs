using NeuroAccessMaui.UI.Pages.Main.QR;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;

namespace NeuroAccessMaui.UI.Pages.Main;

public partial class AppShell : Shell
{
	public AppShell()
	{
		this.InitializeComponent();
		this.RegisterRoutes();
	}
	private void RegisterRoutes()
	{
		// General:
		Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));
		Routing.RegisterRoute(nameof(VerifyCodePage), typeof(VerifyCodePage));
	}
}
