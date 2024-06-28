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

		/// <summary>
		/// Gets the size of the background for the checkmark.
		/// </summary>
		public double CheckmarkBackgroundSize => 120.0;

		/// <summary>
		/// Gets the size of the background for the checkmark.
		/// </summary>
		public double CheckmarkBackgroundCornerRadius => this.CheckmarkBackgroundSize / 2;
		/// <summary>
		/// Gets the size of the icon for the checkmark.
		/// </summary>
		public double CheckmarkIconSize => 60.0;

        [RelayCommand]
		private void Continue()
		{
			GoToRegistrationStep(RegistrationStep.Complete);
		}
    }
}