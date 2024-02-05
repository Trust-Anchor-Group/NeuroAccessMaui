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
			this.ViewModel = new EDalerUriViewModel(null);
			this.InitializeComponent();
		}
	}
}
