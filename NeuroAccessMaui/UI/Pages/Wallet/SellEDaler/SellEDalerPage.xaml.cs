using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.SellEDaler
{
	/// <summary>
	/// A page that allows the user to sell eDaler.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SellEDalerPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SellEDalerPage"/> class.
		/// </summary>
		public SellEDalerPage()
		{
			this.ContentPageModel = new SellEDalerViewModel(ServiceRef.UiService.PopLatestArgs<SellEDalerNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
