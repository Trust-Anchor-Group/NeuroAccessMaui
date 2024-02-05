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
			this.ViewModel = new EDalerReceivedViewModel();
			this.InitializeComponent();
		}
	}
}
