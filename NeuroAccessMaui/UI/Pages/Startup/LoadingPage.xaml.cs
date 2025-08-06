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
    public partial class LoadingPage : ContentPage, ILifeCycleView
    {
        private readonly ITagProfile tagProfile;
        public LoadingPage(ITagProfile tagProfile)
        {
            this.InitializeComponent();
            this.BindingContext = this;
            this.tagProfile = tagProfile;
        }

        public Task OnDisposeAsync() => Task.CompletedTask;

        public Task OnInitializeAsync() => Task.CompletedTask;

        public async Task OnAppearingAsync()
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

        public Task OnDisappearingAsync() => Task.CompletedTask;
    }
}
