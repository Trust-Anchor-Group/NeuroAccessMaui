namespace NeuroAccessMaui.UI.Pages.Main.NfcTester
{
	/// <summary>
	/// Displays the standalone travel-document NFC tester.
	/// </summary>
	public partial class NfcTesterPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcTesterPage"/> class.
		/// </summary>
		/// <param name="ViewModel">The page view model.</param>
		public NfcTesterPage(NfcTesterViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
