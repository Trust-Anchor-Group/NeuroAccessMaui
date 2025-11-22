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
	public partial class LoadingPage : BaseContentPage
	{
		private readonly ITagProfile tagProfile;
		private readonly INavigationService navigationService;
		private readonly IThemeService themeService;
		private readonly INotificationServiceV2 notificationService;

		public LoadingPage(ITagProfile TagProfile, INavigationService NavigationService, IThemeService ThemeService, INotificationServiceV2 NotificationService)
		{
			this.InitializeComponent();
			this.BindingContext = this;
			this.tagProfile = TagProfile;
			this.navigationService = NavigationService;
			this.themeService = ThemeService;
			this.notificationService = NotificationService;
		}

		public override Task OnDisposeAsync() => Task.CompletedTask;

		public override Task OnInitializeAsync() => Task.CompletedTask;

		public override async Task OnAppearingAsync()
		{
			App? AppInstance = App.Current;
			if (AppInstance is not null)
				await AppInstance.InitCompleted;

			await ServiceRef.XmppService.WaitForConnectedState(TimeSpan.FromSeconds(3));

			string? PendingIntentUri = await ServiceRef.IntentService.TryGetAndDequeueOnboardingUrl();

			await ServiceRef.IntentService.ProcessQueuedIntentsAsync();

			bool IsOnboarded = this.tagProfile.IsComplete();

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
				await this.themeService.ApplyProviderTheme();
				await this.navigationService.SetRootAsync(nameof(MainPage));
				try
				{
					await this.notificationService.ProcessPendingAsync(CancellationToken.None);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}
		}

		public override Task OnDisappearingAsync() => Task.CompletedTask;
	}
}
