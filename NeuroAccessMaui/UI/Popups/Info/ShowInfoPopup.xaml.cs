using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Info
{
	public partial class ShowInfoPopup : BasePopup
	{
		public ShowInfoPopup(string infoTitle, string infoText)
		{
			BasePopupViewModel viewModel = new BaseShowInfoViewModel(infoTitle, infoText);
			this.InitializeComponent();
			this.BindingContext = viewModel;
		}

		public ShowInfoPopup()
		{
			this.InitializeComponent();
		}
	}
}
