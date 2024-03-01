using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Main.Settings;

namespace NeuroAccessMaui.UI.Pages.Main
{
	/// <summary>
	/// View model for the <see cref="AppShell"/>.
	/// </summary>
	public partial class AppShellViewModel : BaseViewModel
	{
		/// <summary>
		/// View model for the <see cref="AppShell"/>.
		/// </summary>
		public AppShellViewModel()
		{
		}

		/// <summary>
		/// Closes the app.
		/// </summary>
		/// <returns></returns>
		[RelayCommand]
		private static async Task Close()
		{
			try
			{
				AppShell.Current.FlyoutIsPresented = false;
				await App.Stop();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Views the user's ID.
		/// </summary>
		[RelayCommand]
		private static async Task ViewId()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Shows the settings page.
		/// </summary>
		[RelayCommand]
		private static async Task ShowSettings()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(SettingsPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Shows the applications page.
		/// </summary>
		[RelayCommand]
		private static async Task ShowApplications()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(ApplicationsPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
