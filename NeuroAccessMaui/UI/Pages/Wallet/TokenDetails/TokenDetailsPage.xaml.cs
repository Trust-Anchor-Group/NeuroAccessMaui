using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A page that allows the user to view information about a token.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenDetailsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenDetailsPage"/> class.
		/// </summary>
		public TokenDetailsPage()
		{
			this.ContentPageModel = new TokenDetailsViewModel(ServiceRef.NavigationService.PopLatestArgs<TokenDetailsNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
