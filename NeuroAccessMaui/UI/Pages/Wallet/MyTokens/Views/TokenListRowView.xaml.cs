namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.Views
{
	/// <summary>
	/// A token as a row of a list card: its glyph with any unread marker, its name, expiry and category, and its value.
	/// The binding context is a <see cref="MyWallet.ObjectModels.TokenItem"/>, and the list sets its card position.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenListRowView : ContentView
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenListRowView"/> class.
		/// </summary>
		public TokenListRowView()
		{
			this.InitializeComponent();
		}
	}
}
