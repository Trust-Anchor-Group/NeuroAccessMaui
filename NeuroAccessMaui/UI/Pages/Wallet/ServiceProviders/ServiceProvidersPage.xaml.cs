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
			: this(ServiceRef.NavigationService.PopLatestArgs<ServiceProvidersNavigationArgs>())
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersPage"/> class.
		/// </summary>
		/// <param name="e">Navigation arguments.</param>
		public ServiceProvidersPage(ServiceProvidersNavigationArgs? e)
		{
			this.ContentPageModel = new ServiceProvidersViewModel(e);
			this.InitializeComponent();
		}
	}
}
