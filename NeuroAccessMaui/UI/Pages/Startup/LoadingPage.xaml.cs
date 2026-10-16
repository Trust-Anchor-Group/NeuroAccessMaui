using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;          // for ServiceRef and ServiceHelper
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Theme;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Main;           // for MainPage
using NeuroAccessMaui.UI.Pages.Onboarding;

namespace NeuroAccessMaui.UI.Pages.Startup
{
	/// <summary>
	/// First page shown at startup. Chooses the first real page as soon as local state allows.
	/// </summary>
	public partial class LoadingPage : BaseContentPage
	{
		private static readonly TimeSpan queuedIntentConnectionTimeout = TimeSpan.FromSeconds(3);

		private readonly ITagProfile tagProfile;
		private readonly INavigationService navigationService;
		private readonly IThemeService themeService;
		private readonly INotificationServiceV2 notificationService;

		/// <summary>
		/// Initializes a new instance of the <see cref="LoadingPage"/> class.
		/// </summary>
		/// <param name="TagProfile">The user profile.</param>
		/// <param name="NavigationService">Navigation service used to set the first page.</param>
		/// <param name="ThemeService">Theme service that applies provider branding.</param>
		/// <param name="NotificationService">Notification service whose deferred routes run after the first page is shown.</param>
		public LoadingPage(ITagProfile TagProfile, INavigationService NavigationService, IThemeService ThemeService, INotificationServiceV2 NotificationService)
		{
			this.InitializeComponent();
			this.BindingContext = this;
			this.tagProfile = TagProfile;
			this.navigationService = NavigationService;
			this.themeService = ThemeService;
			this.notificationService = NotificationService;
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync() => Task.CompletedTask;

		/// <inheritdoc/>
		public override Task OnInitializeAsync() => Task.CompletedTask;

		/// <inheritdoc/>
		/// <remarks>
		/// Navigation does not wait for the network: cached branding is applied before the main page is shown, and a missing or
		/// expired branding cache is fetched in the background, which swaps the theme when it arrives. Links that arrived before
		/// startup are processed after the first page is shown.
		/// </remarks>
		public override async Task OnAppearingAsync()
		{
			bool IsOnboarded;

			try
			{
				await App.WaitForServicesAsync();

				string? PendingIntentUri = await ServiceRef.IntentService.TryGetAndDequeueOnboardingUrl();

				IsOnboarded = this.tagProfile.IsComplete();

				if (!IsOnboarded)
				{
					OnboardingNavigationArgs Args;

					if (PendingIntentUri is not null)
						Args = new OnboardingNavigationArgs(OnboardingScenario.FullSetup, PendingIntentUri);
					else
						Args = new OnboardingNavigationArgs(OnboardingScenario.FullSetup);

					await this.navigationService.SetRootAsync(nameof(OnboardingPage), Args);
				}
				else
				{
					await this.ApplyCachedBrandingAsync();
					await this.navigationService.SetRootAsync(nameof(MainPage));
				}
			}
			finally
			{
				// Startup work that the first page does not need starts once that page has been chosen.
				App.ScheduleDeferredWork();
			}

			if (IsOnboarded)
			{
				try
				{
					await this.notificationService.ProcessPendingAsync(CancellationToken.None);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			_ = ProcessQueuedIntentsAsync(IsOnboarded);
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync() => Task.CompletedTask;

		/// <summary>
		/// Applies cached provider branding, and starts a background fetch when the cache is missing or expired.
		/// </summary>
		/// <remarks>
		/// Branding never blocks startup: a failure is logged and the bundled theme stays in place.
		/// </remarks>
		/// <returns>A task that completes when cached branding, if any, has been applied.</returns>
		private async Task ApplyCachedBrandingAsync()
		{
			try
			{
				await this.themeService.ApplyProviderThemeAsync(ThemeFetchPolicy.BackgroundRefresh, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Processes links and other intents that arrived before startup completed.
		/// </summary>
		/// <remarks>
		/// For an onboarded profile, a link may need the server, so the XMPP connection gets a moment to come up first. This no
		/// longer delays the first page.
		/// </remarks>
		/// <param name="IsOnboarded">Whether the profile has completed onboarding.</param>
		/// <returns>A task that completes when the queued intents have been processed.</returns>
		private static async Task ProcessQueuedIntentsAsync(bool IsOnboarded)
		{
			try
			{
				if (IsOnboarded)
					await ServiceRef.XmppService.WaitForConnectedState(queuedIntentConnectionTimeout);

				await ServiceRef.IntentService.ProcessQueuedIntentsAsync();
			}
			catch (Exception Ex) when (App.IsLifecycleException(Ex))
			{
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
