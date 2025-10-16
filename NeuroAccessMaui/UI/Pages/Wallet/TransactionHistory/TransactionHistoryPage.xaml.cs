using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory
{
	/// <summary>
	/// Page presenting the user's transaction history.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TransactionHistoryPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TransactionHistoryPage"/> class.
		/// </summary>
		public TransactionHistoryPage()
		{
			this.ContentPageModel = new TransactionHistoryViewModel();
			this.InitializeComponent();
		}
	}
}
