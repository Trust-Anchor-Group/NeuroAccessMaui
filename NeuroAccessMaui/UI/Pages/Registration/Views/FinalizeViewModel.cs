using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
    public partial class FinalizeViewModel : BaseRegistrationViewModel
	{
		public FinalizeViewModel()
			: base(RegistrationStep.Finalize)
		{
		}

        [RelayCommand]
		private void Continue()
		{
			GoToRegistrationStep(RegistrationStep.Complete);
		}
    }
}