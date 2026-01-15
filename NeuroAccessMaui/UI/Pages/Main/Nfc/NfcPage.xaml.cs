using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Pages.Main.Nfc
{
	/// <summary>
	/// NFC app page providing Read/Write functionality.
	/// </summary>
	public partial class NfcPage : BaseContentPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model instance.</param>
		public NfcPage(NfcViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
