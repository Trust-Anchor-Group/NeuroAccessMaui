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
        public LoadingPage(ITagProfile tagProfile)
        {
            this.InitializeComponent();
            this.BindingContext = this;
            this.tagProfile = tagProfile;
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

            CustomShell Shell = ServiceHelper.GetService<CustomShell>();
            if (!IsOnboarded)
                await Shell.SetPageAsync(ServiceHelper.GetService<RegistrationPage>());
            else
                await Shell.SetPageAsync(ServiceHelper.GetService<MainPage>());
        }

        public override Task OnDisappearingAsync() => Task.CompletedTask;
    }
}
