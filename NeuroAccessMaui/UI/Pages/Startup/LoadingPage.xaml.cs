using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Registration;    // for RegistrationPage
using NeuroAccessMaui.UI.Pages.Main;           // for MainPage
using NeuroAccessMaui.Test;                    // for CustomShell
using NeuroAccessMaui.Services;          // for ServiceRef and ServiceHelper
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Theme;
using NeuroAccessMaui.UI.Pages.Onboarding;

namespace NeuroAccessMaui.UI.Pages.Startup
{
    public partial class LoadingPage : BaseContentPage
    {
        private readonly ITagProfile tagProfile;
		private readonly INavigationService navigationService;
		private readonly IThemeService themeService;

		public LoadingPage(ITagProfile tagProfile, INavigationService navigationService, IThemeService themeService)
        {
            this.InitializeComponent();
            this.BindingContext = this;
            this.tagProfile = tagProfile;
            this.navigationService = navigationService;
            this.themeService = themeService;
        }

        public override Task OnDisposeAsync() => Task.CompletedTask;

        public override Task OnInitializeAsync() => Task.CompletedTask;

        public override async Task OnAppearingAsync()
        {
            App? app = App.Current;
            if (app is not null)
                await app.InitCompleted;

            bool isOnboarded = this.tagProfile.IsComplete();

            if (!isOnboarded)
            {
                await this.navigationService.SetRootAsync(nameof(OnboardingPage), new OnboardingNavigationArgs { InitialStep = OnboardingStep.PinSetup });
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
