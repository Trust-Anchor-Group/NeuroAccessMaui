using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Permission
{
	public partial class ShowPermissionPopup : BasePopup
	{
		public ShowPermissionPopup(ShowPermissionViewModel vm)
		{
			this.InitializeComponent();
			this.BindingContext = vm;
		}
	}
}
