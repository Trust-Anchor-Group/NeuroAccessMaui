using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Permission
{
	public partial class ShowPermissionPopup
	{
		public ShowPermissionPopup(string Title, string Text)
		{
			BasePopupViewModel viewModel = new BaseShowPermissionViewModel(Title, Text);

			this.InitializeComponent();
			this.BindingContext = viewModel;
		}

		public ShowPermissionPopup()
		{
			this.InitializeComponent();
		}


	}
}
