namespace NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler
{
	/// <summary>
	/// A page that allows the user to buy eDaler.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BuyEDalerPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="BuyEDalerPage"/> class.
		/// </summary>
		public BuyEDalerPage()
		{
			this.ContentPageModel = new BuyEDalerViewModel();
			this.InitializeComponent();
		}
	}
}
