using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Identity;
using NeuroAccessMaui.UI.Pages.Main.QR;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Pages.Petitions;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class AppShell : Shell
	{
		public AppShell()
		{
			this.InitializeComponent();
			this.RegisterRoutes();

			ServiceRef.TagProfile.SetTheme();
		}

		private void RegisterRoutes()
		{
			// General:
			Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));
			Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
			Routing.RegisterRoute(nameof(ChangePinPage), typeof(ChangePinPage));
			Routing.RegisterRoute(nameof(VerifyCodePage), typeof(VerifyCodePage));
			Routing.RegisterRoute(nameof(PetitionSignaturePage), typeof(PetitionSignaturePage));
			Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));
		}

		private async void Close_Clicked(object sender, EventArgs e)
		{
			try
			{
				Current.FlyoutIsPresented = false;
				await App.Stop();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async void ViewId_Clicked(object sender, EventArgs e)
		{
			// Note: Implementing as FlyoutItem only instantiates view one time.
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async void Settings_Clicked(object sender, EventArgs e)
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(SettingsPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
