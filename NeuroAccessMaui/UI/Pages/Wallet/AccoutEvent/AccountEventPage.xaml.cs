using Xamarin.Forms.Xaml;

namespace NeuroAccessMaui.UI.Pages.Wallet.AccountEvent
{
    /// <summary>
    /// A page that allows the user to view information about an account event.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AccountEventPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AccountEventPage"/> class.
        /// </summary>
		public AccountEventPage()
		{
            this.ViewModel = new AccountEventViewModel();

			this.InitializeComponent();
        }
    }
}
