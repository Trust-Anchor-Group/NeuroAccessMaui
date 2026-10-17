namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet.Views
{
	/// <summary>
	/// The Tokens section of the Wallet tab: totals per currency, the most recent tokens, and a link to all tokens.
	/// The binding context is the <see cref="WalletViewModel"/>.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WalletTokensView : BaseContentView
	{
		/// <summary>
		/// Creates a new instance of the <see cref="WalletTokensView"/> class.
		/// </summary>
		public WalletTokensView()
		{
			this.InitializeComponent();
		}
	}
}
