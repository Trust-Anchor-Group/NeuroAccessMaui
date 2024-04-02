using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.PaymentAcceptance
{
	/// <summary>
	/// A page that allows the user to accept an offline payment.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PaymentAcceptancePage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PaymentAcceptancePage"/> class.
		/// </summary>
		public PaymentAcceptancePage()
		{
			this.ContentPageModel = new EDalerUriViewModel(null, ServiceRef.UiService.PopLatestArgs<EDalerUriNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
