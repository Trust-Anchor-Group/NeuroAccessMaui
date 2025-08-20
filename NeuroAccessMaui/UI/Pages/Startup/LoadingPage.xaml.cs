using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Registration;    // for RegistrationPage
using NeuroAccessMaui.UI.Pages.Main;           // for MainPage
using NeuroAccessMaui.Test;                    // for CustomShell
using NeuroAccessMaui.Services;          // for ServiceRef and ServiceHelper
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Startup
{
    public partial class LoadingPage : BaseContentPage
    {
        private readonly ITagProfile tagProfile;
		private readonly INavigationService navigationService;
		public LoadingPage(ITagProfile tagProfile, INavigationService navigationService)
        {
            this.InitializeComponent();
            this.BindingContext = this;
            this.tagProfile = tagProfile;
            this.navigationService = navigationService;
        }

        public override Task OnDisposeAsync() => Task.CompletedTask;

        public override Task OnInitializeAsync() => Task.CompletedTask;

        public override async Task OnAppearingAsync()
        {
            App? App = App.Current;
            if (App is not null)
                await App.InitCompleted;
            // Check onboarding
            bool IsOnboarded = this.tagProfile.IsComplete();

			if (!IsOnboarded)
				await this.navigationService.GoToAsync(nameof(RegistrationPage));
			else
				await this.navigationService.GoToAsync(nameof(MainPage));
        }

        public override Task OnDisappearingAsync() => Task.CompletedTask;
    }
}
