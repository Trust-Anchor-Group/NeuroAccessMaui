using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// A page that allows the user to view the contents of its eDaler wallet, pending payments and recent account events.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyEDalerWalletPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyEDalerWalletPage"/> class.
		/// </summary>
		public MyEDalerWalletPage()
		{
			this.ContentPageModel = new MyWalletViewModel(ServiceRef.NavigationService.PopLatestArgs<WalletNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
