using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
    public partial class FinalizeOnboardingStepViewModel : BaseOnboardingStepViewModel
    {
        public FinalizeOnboardingStepViewModel() : base(OnboardingStep.Finalize) { }

        public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingFinalizePageTitle)];

        public override string Description => ServiceRef.Localizer[nameof(AppResources.SuccessTitle)];

        public double CheckmarkBackgroundSize => 120.0;

        public RoundRectangle CheckmarkBackgroundStroke => new RoundRectangle { CornerRadius = this.CheckmarkBackgroundSize / 2 };

        public double CheckmarkIconSize => 60.0;

        [RelayCommand]
        private async Task Continue()
        {
            if (this.CoordinatorViewModel is not null)
                await this.CoordinatorViewModel.GoToNextCommand.ExecuteAsync(null);
        }
    }
}
