namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.Views
{
	/// <summary>
	/// A row showing a token in a list: its glyph with any unread marker, its name, category and expiry, and its value.
	/// The binding context is a <see cref="MyWallet.ObjectModels.TokenItem"/>.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenRowView : ContentView
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenRowView"/> class.
		/// </summary>
		public TokenRowView()
		{
			this.InitializeComponent();
		}
	}
}
