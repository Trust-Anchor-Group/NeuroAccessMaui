using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

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
			List<string> AllowedSchemas = [Constants.UriSchemes.TagSign];

			if (ServiceRef.TagProfile.LegalIdentity?.HasApprovedPersonalInformation() ?? false)
			{
				AllowedSchemas.Add(Constants.UriSchemes.IotId);

				// TODO:
				// AllowedSchemas.Add(Constants.UriSchemes.IotSc);
				// AllowedSchemas.Add(Constants.UriSchemes.IotDisco);
				// AllowedSchemas.Add(Constants.UriSchemes.EDaler);
				// AllowedSchemas.Add(Constants.UriSchemes.NeuroFeature);
				// AllowedSchemas.Add(Constants.UriSchemes.Xmpp);
			}

			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrScanCode), [.. AllowedSchemas]);
			if (string.IsNullOrEmpty(Url))
				return;

			await App.OpenUrlAsync(Url);
		}
	}
}
