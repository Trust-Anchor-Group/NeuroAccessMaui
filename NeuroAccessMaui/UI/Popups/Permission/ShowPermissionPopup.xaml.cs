using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Permission
{
	public partial class ShowPermissionPopup
	{
		public ShowPermissionPopup(string Title, string Description, string DescriptionSecondary)
		{
			BasePopupViewModel viewModel = new BaseShowPermissionViewModel(Title, Description, DescriptionSecondary);

			this.InitializeComponent();
			this.BindingContext = viewModel;
		}

		public ShowPermissionPopup()
		{
			this.InitializeComponent();
		}


	}
}
