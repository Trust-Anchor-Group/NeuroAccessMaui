namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails.Views
{
	/// <summary>
	/// The state machine sections of token details: the first few variables, the note commands that apply now, and the
	/// reports. The current state is a chip in the hero.
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
