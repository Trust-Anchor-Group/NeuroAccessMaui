namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// A page that allows the user to view its tokens.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyTokensPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyTokensPage"/> class.
		/// </summary>
		public MyTokensPage()
		{
			this.ContentPageModel = new MyTokensViewModel();
			this.InitializeComponent();
		}
	}
}
