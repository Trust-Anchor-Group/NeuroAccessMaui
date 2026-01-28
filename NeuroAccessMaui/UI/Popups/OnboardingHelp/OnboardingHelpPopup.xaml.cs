using NeuroAccessMaui.UI.Popups.Photos.Image;

namespace NeuroAccessMaui.UI.Popups.OnboardingHelp
{

	public partial class OnboardingHelpPopup : SimplePopup
	{
		public OnboardingHelpPopup()
		{
			this.InitializeComponent();

			this.BindingContext = new OnboardingHelpViewModel();
		}
	}
}
