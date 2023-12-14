using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;

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
			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanInvitation),
				[
					Constants.UriSchemes.TagSign		// TODO: Add other schemas, as user unlocks features.
				]);

			if (string.IsNullOrEmpty(Url))
				return;

			await App.OpenUrlAsync(Url);
		}
	}
}
