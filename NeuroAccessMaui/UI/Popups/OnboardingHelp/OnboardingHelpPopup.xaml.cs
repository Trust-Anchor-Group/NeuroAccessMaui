using NeuroAccessMaui.UI.Popups.Photos.Image;

namespace NeuroAccessMaui.UI.Popups.OnboardingHelp
{

	public partial class OnboardingHelpPopup : BasePopup
	{
		public OnboardingHelpPopup()
		{
			this.InitializeComponent();

			this.BindingContext = new OnboardingHelpViewModel();
		}
	}
}
