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
using NeuroAccessMaui.Services.Data; // Added for Database access
using System.Linq;
using Waher.Persistence;
using Waher.Persistence.Filters;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Tag; // Added for ordering
using NeuroAccessMaui.CustomPermissions;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Notification;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class HomeViewModel : QrXmppViewModel
	{
		private readonly IAuthenticationService authenticationService = ServiceRef.AuthenticationService;
		private readonly INotificationServiceV2 notificationService;

		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerLargeLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerLargeDark);

		[ObservableProperty]
		bool themeLoaded = false;

		private IdentityState? latestCreatedIdentityState; // Cached state from latest stored KYC reference
		private ApplicationReview? latestApplicationReview;
		private bool reviewEventSubscribed;

		public string BannerUri =>
			Application.Current?.UserAppTheme switch
			{
				AppTheme.Dark => this.BannerUriDark,
				AppTheme.Light => this.BannerUriLight,
				_ => this.BannerUriLight
			} ?? this.BannerUriLight;

		public HomeViewModel()
			: base()
		{
			this.notificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();

			Application.Current.RequestedThemeChanged += (_, __) =>
				OnPropertyChanged(nameof(BannerUri));
		}

		public override Task<string> Title => Task.FromResult(ContactInfo.GetFriendlyName(ServiceRef.TagProfile.LegalIdentity));

		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			try
			{
				// Load latest stored KYC reference state
				await this.LoadLatestKycStateAsync();
				
				try
				{
					if (!await ServiceRef.SettingsService.RestoreBoolState(Constants.Settings.PushNotificationAsked, false))
						await ServiceRef.PermissionService.CheckNotificationPermissionAsync();

					await ServiceRef.SettingsService.SaveState(Constants.Settings.PushNotificationAsked, true);
				}
				catch
				{
					//Normal operation if Notification is not supported or denied
				}
				
				_ = await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect);
				await ServiceRef.ThemeService.ThemeLoaded.Task;
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.ThemeLoaded = true;
					this.OnPropertyChanged(nameof(this.BannerUri));
				});
				await ServiceRef.IntentService.ProcessQueuedIntentsAsync();


				//		GeoMapViewModel vm = new(59.638346832492765,11.879682074310969);
				//		await ServiceRef.PopupService.PushAsync(new GeoMapPopup(vm));
				//		Console.WriteLine($"GeoMap result: {await vm.Result}");

			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged += this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;
			ServiceRef.TagProfile.Changed += this.TagProfile_PropertyChanged;
			if (!this.reviewEventSubscribed)
			{
				ServiceRef.KycService.ApplicationReviewUpdated += this.KycService_ApplicationReviewUpdated;
				this.reviewEventSubscribed = true;
			}

			this.notificationService.OnNotificationAdded += this.NotificationService_OnNotificationAdded;
			await this.RefreshUnreadNotificationsAsync();
		}

		public override Task OnDisappearingAsync()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged -= this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;
			ServiceRef.TagProfile.Changed -= this.TagProfile_PropertyChanged;
			if (this.reviewEventSubscribed)
			{
				ServiceRef.KycService.ApplicationReviewUpdated -= this.KycService_ApplicationReviewUpdated;
				this.reviewEventSubscribed = false;
			}
			this.notificationService.OnNotificationAdded -= this.NotificationService_OnNotificationAdded;
			return base.OnDisappearingAsync();
		}

		private void TagProfile_OnPropertiesChanged(object? Sender, EventArgs e)
		{
			Task.Run(this.LoadLatestKycStateAsync);
		}

		private void TagProfile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.PropertyName) ||
				e.PropertyName == nameof(ITagProfile.IdentityApplication) ||
				e.PropertyName == nameof(ITagProfile.LegalIdentity))
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.OnPropertyChanged(nameof(this.HasPersonalIdentity));
					this.OnPropertyChanged(nameof(this.HasPendingIdentity));
					this.OnPropertyChanged(nameof(this.ShowApplyIdBox));
					this.OnPropertyChanged(nameof(this.ShowPendingIdBox));
					this.OnPropertyChanged(nameof(this.ShowRejectedIdBox));
					this.OnPropertyChanged(nameof(this.ShowInfoBubble));
					this.OnPropertyChanged(nameof(this.ShowIdButtonText));
				});
			}
		}

		private async Task XmppService_IdentityApplicationChanged(object? Sender, EventArgs e)
		{
			// Refresh cached KYC state when identity application changes
			await this.LoadLatestKycStateAsync();
		}

		private async Task XmppService_LegalIdentityChanged(object? Sender, EventArgs e)
		{
			await this.LoadLatestKycStateAsync();
		}

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

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

		public string ShowIdButtonText => this.HasPersonalIdentity ? ServiceRef.Localizer[nameof(AppResources.ShowIDShort)] : ServiceRef.Localizer[nameof(AppResources.ShowAccount)];

		public bool HasPersonalIdentity => ServiceRef.TagProfile.LegalIdentity?.HasApprovedPersonalInformation() ?? false;
		public bool HasPendingIdentity => this.CheckPendingIdentity();

		public bool ShowInfoBubble => this.ShowApplyIdBox || this.ShowPendingIdBox || this.ShowRejectedIdBox;
		public bool ShowApplyIdBox => !(ServiceRef.TagProfile.LegalIdentity?.HasApprovedPersonalInformation() ?? false) && !this.CheckPendingIdentity() && !this.CheckRejectedIdentity();
		// Only show pending while the application is in Created (review may be ongoing but not rejected).
		public bool ShowPendingIdBox => this.CheckPendingIdentity();
		public bool ShowRejectedIdBox => this.CheckRejectedIdentity();

		private bool CheckPendingIdentity()
		{
			IdentityState? state = this.latestCreatedIdentityState ?? ServiceRef.TagProfile.IdentityApplication?.State;
			return state == IdentityState.Created;
		}

		private bool CheckRejectedIdentity()
		{
			IdentityState? state = this.latestCreatedIdentityState ?? ServiceRef.TagProfile.IdentityApplication?.State;
			return state == IdentityState.Rejected;
		}

		private Task NotificationService_OnNotificationAdded(object? Sender, NotificationRecordEventArgs e)
		{
			return this.RefreshUnreadNotificationsAsync();
		}

		private async Task RefreshUnreadNotificationsAsync()
		{
			try
			{
				NotificationQuery Query = new()
				{
					States = new List<NotificationState>
					{
						NotificationState.New,
						NotificationState.Delivered
					}
				};

				IReadOnlyList<NotificationRecord> Records = await this.notificationService.GetAsync(Query, CancellationToken.None);
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.UnreadNotificationCount = Records.Count;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Gets a value indicating whether there are unread notifications.
		/// </summary>
		public bool HasUnreadNotifications => this.UnreadNotificationCount > 0;

		/// <summary>
		/// Gets or sets the unread notification count.
		/// </summary>
		[ObservableProperty]
		private int unreadNotificationCount;

		partial void OnUnreadNotificationCountChanged(int value)
		{
			this.OnPropertyChanged(nameof(this.HasUnreadNotifications));
		}

		private async Task LoadLatestKycStateAsync()
		{
			try
			{
				KycReference? LatestReference = await FindMostRecentReferenceAsync();
				this.latestCreatedIdentityState = LatestReference?.CreatedIdentityState;
				this.latestApplicationReview = LatestReference?.ApplicationReview;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.OnPropertyChanged(nameof(this.HasPersonalIdentity));
					this.OnPropertyChanged(nameof(this.HasPendingIdentity));
					this.OnPropertyChanged(nameof(this.ShowPendingIdBox));
					this.OnPropertyChanged(nameof(this.ShowApplyIdBox));
					this.OnPropertyChanged(nameof(this.ShowRejectedIdBox));
					this.OnPropertyChanged(nameof(this.ShowInfoBubble));
					this.OnPropertyChanged(nameof(this.ShowIdButtonText));
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void KycService_ApplicationReviewUpdated(object? sender, ApplicationReviewEventArgs e)
		{
			this.latestApplicationReview = e.Review;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.ShowInfoBubble));
				this.OnPropertyChanged(nameof(this.ShowPendingIdBox));
				this.OnPropertyChanged(nameof(this.ShowRejectedIdBox));
				this.OnPropertyChanged(nameof(this.ShowApplyIdBox));
				this.OnPropertyChanged(nameof(this.ShowIdButtonText));
			});
		}

		private static async Task<KycReference?> FindMostRecentReferenceAsync()
		{
			LegalIdentity? IdentityApplication = ServiceRef.TagProfile.IdentityApplication;
			if (IdentityApplication is not null)
			{
				try
				{
					KycReference? Match = await Database.FindFirstIgnoreRest<KycReference>(new FilterFieldEqualTo(nameof(KycReference.CreatedIdentityId), IdentityApplication.Id));
					if (Match is not null)
						return Match;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			try
			{
				IEnumerable<KycReference> All = await Database.Find<KycReference>();
				return All.OrderByDescending(r => r.UpdatedUtc).FirstOrDefault();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return null;
		}

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
				if (await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.ViewId))
					await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage));
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
				await ServiceRef.NavigationService.GoToAsync(nameof(NotificationsPage));
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
				await ServiceRef.NavigationService.GoToAsync(nameof(ApplicationsPage));
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
				await ServiceRef.NavigationService.GoToAsync(nameof(AppsPage));
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

				await ServiceRef.NavigationService.GoToAsync(nameof(WalletPage), Args, BackMethod.Pop);
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
				await ServiceRef.NavigationService.GoToAsync(nameof(SettingsPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
