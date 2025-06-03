using System.ComponentModel;
using System.Diagnostics.Contracts;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.CustomPermissions;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Notifications;
using Waher.Networking.XMPP.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.Extensions;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainViewModel : QrXmppViewModel
	{


		public MainViewModel()
			: base()
		{
		}

		public override Task<string> Title => Task.FromResult(ContactInfo.GetFriendlyName(ServiceRef.TagProfile.LegalIdentity));

		protected override async Task OnAppearing()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.HasPersonalIdentity));
			});

			await base.OnAppearing();
			try
			{
				/*
				try
				{
					await Permissions.RequestAsync<NotificationPermission>();
				}
				catch
				{
					//Normal operation if Notification is not supported or denied
				}
				*/
				_ = await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect);
				await ServiceRef.IntentService.ProcessQueuedIntentsAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			await this.OnIsConnectedChanged(); // Call this method in case the connection state has already changed before the view model was initialized.
		}

		protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsConnected):
					await this.OnIsConnectedChanged();
					break;
			}
		}

		private async Task OnIsConnectedChanged()
		{
			try
			{
				if (this.IsConnected && ServiceRef.TagProfile.LegalIdentityNeedsRefreshing())
				{
					LegalIdentity RefreshedIdentity = await ServiceRef.XmppService.GetLegalIdentity(ServiceRef.TagProfile.LegalIdentity?.Id);
					await MainThread.InvokeOnMainThreadAsync(async () => await ServiceRef.TagProfile.SetLegalIdentity(RefreshedIdentity, false));
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.ScanQrCodeCommand.NotifyCanExecuteChanged();
			}
		}

		public bool HasPersonalIdentity => ServiceRef.TagProfile.LegalIdentity?.HasApprovedPersonalInformation() ?? false;

		public bool CanScanQrCode => true;

		[RelayCommand(CanExecute = nameof(CanScanQrCode))]
		private async Task ScanQrCode()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult();
			});
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		public async Task ViewId()
		{
			try
			{
				if(await App.AuthenticateUserAsync(AuthenticationPurpose.ViewId))
					await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		public async Task OpenNotifications()
		{
			try
			{
				if (await App.AuthenticateUserAsync(AuthenticationPurpose.ViewId))
					await ServiceRef.UiService.GoToAsync(nameof(NotificationsPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		public async Task GoToApplyIdentity()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(ApplyIdPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
