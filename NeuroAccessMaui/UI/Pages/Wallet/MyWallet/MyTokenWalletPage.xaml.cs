namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// A page that allows the user to view the contents of its token wallet, pending payments and recent account events.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyTokenWalletPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyTokenWalletPage"/> class.
		/// </summary>
		public MyTokenWalletPage()
		{
			this.ViewModel = new MyWalletViewModel(this);
			this.InitializeComponent();
		}
	}
}
