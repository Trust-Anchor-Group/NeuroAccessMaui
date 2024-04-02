using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.PendingPayment
{
	/// <summary>
	/// A page that allows the user to view information about a pending payment.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PendingPaymentPage : IShareQrCode
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PendingPaymentPage"/> class.
		/// </summary>
		public PendingPaymentPage()
		{
			this.ContentPageModel = new EDalerUriViewModel(this, ServiceRef.UiService.PopLatestArgs<EDalerUriNavigationArgs>());
			this.InitializeComponent();
		}

		/// <summary>
		/// Scrolls to display the QR-code.
		/// </summary>
		public async Task ShowQrCode()
		{
			await this.ScrollView.ScrollToAsync(this.ShareButton, ScrollToPosition.MakeVisible, true);
		}
	}
}
