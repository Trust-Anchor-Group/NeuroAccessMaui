namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails.Views
{
	/// <summary>
	/// The state machine section of token details: the current state, the first few variables, and the reports.
	/// The binding context is the <see cref="TokenDetailsViewModel"/>.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenStateMachineView : ContentView
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenStateMachineView"/> class.
		/// </summary>
		public TokenStateMachineView()
		{
			this.InitializeComponent();
		}
	}
}
