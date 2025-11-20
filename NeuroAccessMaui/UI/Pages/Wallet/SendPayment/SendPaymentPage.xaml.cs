using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.SendPayment
{
	/// <summary>
	/// A page that allows the user to realize payments.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SendPaymentPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SendPaymentPage"/> class.
		/// </summary>
		public SendPaymentPage()
		{
			this.ContentPageModel = new EDalerUriViewModel(null, ServiceRef.NavigationService.PopLatestArgs<EDalerUriNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
