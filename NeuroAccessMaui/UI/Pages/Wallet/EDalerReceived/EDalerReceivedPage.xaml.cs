using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.EDalerReceived
{
	/// <summary>
	/// A page that displays information about eDaler received.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EDalerReceivedPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="EDalerReceivedPage"/> class.
		/// </summary>
		public EDalerReceivedPage()
		{
			this.ContentPageModel = new EDalerReceivedViewModel(ServiceRef.UiService.PopLatestArgs<EDalerBalanceNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
