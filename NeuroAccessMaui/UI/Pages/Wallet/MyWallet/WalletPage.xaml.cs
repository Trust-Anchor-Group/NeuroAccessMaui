namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// A page that allows the user to view the contents of its eDaler wallet, pending payments and recent account events.
	/// </summary>
	public partial class WalletPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyEDalerWalletPage"/> class.
		/// </summary>
		public WalletPage(WalletViewModel ViewModel)
		{
			this.BindingContext = ViewModel;
			this.InitializeComponent();
		}
	}
}
