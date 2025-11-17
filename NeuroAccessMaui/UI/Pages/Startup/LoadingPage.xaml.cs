using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Registration;    // for RegistrationPage
using NeuroAccessMaui.UI.Pages.Main;           // for MainPage
using NeuroAccessMaui.Services;          // for ServiceRef and ServiceHelper
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Theme;
using NeuroAccessMaui.UI.Pages.Onboarding;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Startup
{
	public partial class LoadingPage : BaseContentPage
	{
		private readonly ITagProfile tagProfile;
		private readonly INavigationService navigationService;
		private readonly IThemeService themeService;

		public LoadingPage(ITagProfile TagProfile, INavigationService NavigationService, IThemeService ThemeService)
		{
			this.InitializeComponent();
			this.BindingContext = this;
			this.tagProfile = TagProfile;
			this.navigationService = NavigationService;
			this.themeService = ThemeService;
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

			//TODO: Handle the case wher an intent to URI before onboarding is completed

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
			}
		}

		public override Task OnDisappearingAsync() => Task.CompletedTask;
	}
}
