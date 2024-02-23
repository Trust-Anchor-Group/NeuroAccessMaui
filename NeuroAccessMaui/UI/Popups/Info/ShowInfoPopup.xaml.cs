using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Info
{
	public partial class ShowInfoPopup
	{
		public ShowInfoPopup(string InfoTitle, string InfoText)
		{
			BasePopupViewModel viewModel = new BaseShowInfoViewModel(InfoTitle, InfoText);

			this.InitializeComponent();
			this.BindingContext = viewModel;
		}

		public ShowInfoPopup()
		{
			this.InitializeComponent();
		}


	}
}
