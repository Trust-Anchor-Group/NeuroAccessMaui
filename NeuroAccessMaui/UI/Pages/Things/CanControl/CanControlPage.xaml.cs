using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Things.CanControl
{
	/// <summary>
	/// A page that asks the user if a remote entity is allowed to control the device.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CanControlPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="CanControlPage"/> class.
		/// </summary>
		public CanControlPage()
		{
			this.ContentPageModel = new CanControlViewModel(ServiceRef.NavigationService.PopLatestArgs<CanControlNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
