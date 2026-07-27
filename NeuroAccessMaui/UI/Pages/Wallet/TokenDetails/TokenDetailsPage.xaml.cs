using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Presents a progressively disclosed token workspace.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenDetailsPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TokenDetailsPage"/> class.
		/// </summary>
		public TokenDetailsPage()
		{
			this.ContentPageModel = new TokenDetailsViewModel(
				ServiceRef.NavigationService.PopLatestArgs<TokenDetailsNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
