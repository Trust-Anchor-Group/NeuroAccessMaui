using Xamarin.Forms.Xaml;

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
            this.ViewModel = new EDalerUriViewModel(null);

			this.InitializeComponent();
        }
    }
}
