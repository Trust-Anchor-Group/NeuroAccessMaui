using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Notifications;
using Waher.Networking.XMPP.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Pages.Kyc;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using EDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using System.Globalization;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.UI.Pages.Applications.Applications;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainViewModel : QrXmppViewModel
	{
		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerLargeLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerLargeDark);

		[ObservableProperty]
		bool themeLoaded = false;

		public string BannerUri =>
			Application.Current?.UserAppTheme switch
			{
				AppTheme.Dark => this.BannerUriDark,
				AppTheme.Light => this.BannerUriLight,
				_ => this.BannerUriLight
			} ?? this.BannerUriLight;

		public MainViewModel()
			: base()
		{

			Application.Current.RequestedThemeChanged += (_, __) =>
				OnPropertyChanged(nameof(BannerUri));
		}
	
		public override Task<string> Title => Task.FromResult(ContactInfo.GetFriendlyName(ServiceRef.TagProfile.LegalIdentity));

		protected override async Task OnAppearing()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.HasPersonalIdentity));
				this.OnPropertyChanged(nameof(this.HasPendingIdentity));
				this.OnPropertyChanged(nameof(this.ShowPendingIdBox));
				this.OnPropertyChanged(nameof(this.ShowApplyIdBox));
				this.OnPropertyChanged(nameof(this.ShowInfoBubble));
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
				await ServiceRef.ThemeService.ThemeLoaded.Task;
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.ThemeLoaded = true;
					this.OnPropertyChanged(nameof(this.BannerUri));
				});
				await ServiceRef.IntentService.ProcessQueuedIntentsAsync();


				//		GeoMapViewModel vm = new(59.638346832492765,11.879682074310969);
				//		await ServiceRef.UiService.PushAsync(new GeoMapPopup(vm));
				//		Console.WriteLine($"GeoMap result: {await vm.Result}");

			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged += this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;
		}

		protected override Task OnDispose()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged -= this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;

			return base.OnDispose();
		}

		private void TagProfile_OnPropertiesChanged(object? Sender, EventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.HasPersonalIdentity));
				this.OnPropertyChanged(nameof(this.HasPendingIdentity));
				this.OnPropertyChanged(nameof(this.ShowPendingIdBox));
				this.OnPropertyChanged(nameof(this.ShowApplyIdBox));
				this.OnPropertyChanged(nameof(this.ShowInfoBubble));
			});
		}

		private Task XmppService_LegalIdentityChanged(object? Sender, EventArgs e)
		{
			MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.OnPropertyChanged(nameof(this.HasPersonalIdentity));
				this.OnPropertyChanged(nameof(this.HasPendingIdentity));
				this.OnPropertyChanged(nameof(this.ShowPendingIdBox));
				this.OnPropertyChanged(nameof(this.ShowApplyIdBox));
				this.OnPropertyChanged(nameof(this.ShowInfoBubble));
			});

			return Task.CompletedTask;
		}

		private Task XmppService_IdentityApplicationChanged(object? Sender, EventArgs e)
		{
			MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.OnPropertyChanged(nameof(this.HasPersonalIdentity));
				this.OnPropertyChanged(nameof(this.HasPendingIdentity));
				this.OnPropertyChanged(nameof(this.ShowPendingIdBox));
				this.OnPropertyChanged(nameof(this.ShowApplyIdBox));
				this.OnPropertyChanged(nameof(this.ShowInfoBubble));
			});

			return Task.CompletedTask;
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
		public bool ShowApplyIdBox => !(ServiceRef.TagProfile.LegalIdentity?.HasApprovedPersonalInformation() ?? false) && !this.CheckPendingIdentity();

		public bool HasPendingIdentity => this.CheckPendingIdentity();
		public bool ShowPendingIdBox => this.CheckPendingIdentity();

		private bool CheckPendingIdentity()
		{
			return ServiceRef.TagProfile.IdentityApplication is LegalIdentity Application && Application.State == IdentityState.Created;
		}

		public bool ShowInfoBubble => this.ShowApplyIdBox || this.ShowPendingIdBox;

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
				await ServiceRef.UiService.GoToAsync(nameof(ApplicationsPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		// Go to Apps page
		[RelayCommand]
		public async Task ViewApps()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(AppsPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[ObservableProperty]
		private bool showingNoWalletPopup = false;

		[RelayCommand(AllowConcurrentExecutions = false)]
		public async Task OpenWallet()
		{
			if (ServiceRef.TagProfile.HasBetaFeatures)
			{
				await ShowWallet();
				return;
			}
			else
			{
				this.ShowingNoWalletPopup = true;
				await Task.Delay(5000);
				this.ShowingNoWalletPopup = false;
			}
		}

		[RelayCommand]
		internal static async Task ShowWallet()
		{
			try
			{
				WalletNavigationArgs Args = new();

				await ServiceRef.UiService.GoToAsync(nameof(WalletPage), Args, BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
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
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
