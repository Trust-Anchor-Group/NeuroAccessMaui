namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.Views
{
	/// <summary>
	/// Tokens of the same kind as one row of a list card: the first token's glyph with the unread badge of all of them,
	/// their name, how many there are, the first expiry and their category, and their value together. The binding context
	/// is a <see cref="ObjectModels.TokenFamilyItem"/>, and a tap unfolds it.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenFamilyRowView : ContentView
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenFamilyRowView"/> class.
		/// </summary>
		public TokenFamilyRowView()
		{
			this.InitializeComponent();
		}
	}
}
