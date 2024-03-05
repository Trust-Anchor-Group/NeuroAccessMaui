using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// A page that allows the user to view its tokens.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ServiceProvidersPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersPage"/> class.
		/// </summary>
		public ServiceProvidersPage()
		{
			this.ContentPageModel = new ServiceProvidersViewModel(ServiceRef.UiService.PopLatestArgs<ServiceProvidersNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
