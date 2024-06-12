using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainViewModel : QrXmppViewModel
	{
		public MainViewModel()
			: base()
		{
		}

		public override Task<string> Title => Task.FromResult(ContactInfo.GetFriendlyName(ServiceRef.TagProfile.LegalIdentity));

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
				if(await App.AuthenticateUser(AuthenticationPurpose.ViewId))
					await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
