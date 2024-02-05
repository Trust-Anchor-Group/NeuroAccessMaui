namespace NeuroAccessMaui.UI.Pages.Wallet.RequestPayment
{
	/// <summary>
	/// A page that displays information about eDaler received.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class RequestPaymentPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="RequestPaymentPage"/> class.
		/// </summary>
		public RequestPaymentPage()
		{
			this.ViewModel = new RequestPaymentViewModel(this);
			this.InitializeComponent();
		}

		/// <summary>
		/// Scrolls to display the QR-code.
		/// </summary>
		public async Task ShowQrCode()
		{
			await this.ScrollView.ScrollToAsync(this.ShareExternalButton, ScrollToPosition.MakeVisible, true);
		}
	}
}
