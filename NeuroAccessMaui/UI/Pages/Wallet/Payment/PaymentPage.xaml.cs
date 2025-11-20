using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.Payment
{
	/// <summary>
	/// A page that allows the user to realize payments.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PaymentPage : IShareQrCode
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PaymentPage"/> class.
		/// </summary>
		public PaymentPage()
		{
			this.ContentPageModel = new EDalerUriViewModel(this, ServiceRef.NavigationService.PopLatestArgs<EDalerUriNavigationArgs>());
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
