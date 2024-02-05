using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainViewModel : QrXmppViewModel
	{
		public MainViewModel()
			: base()
		{
		}

		public override Task<string> Title => Task.FromResult("TBD");  // TODO

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsConnected):
					this.ScanQrCodeCommand.NotifyCanExecuteChanged();
					break;
			}
		}

		public bool CanScanQrCode => this.IsConnected;

		[RelayCommand(CanExecute = nameof(CanScanQrCode))]
		private async Task ScanQrCode()
		{
			await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult();
		}

		[RelayCommand]
		public static async Task ViewId()
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
