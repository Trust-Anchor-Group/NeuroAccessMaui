namespace NeuroAccessMaui.UI.Popups.Nfc
{
	/// <summary>
	/// Popup shown when Safe Scan requires an explicit user decision before opening an NFC link.
	/// </summary>
	public partial class NfcOpenLinkDecisionPopup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcOpenLinkDecisionPopup"/> class.
		/// </summary>
		/// <param name="ViewModel">Popup view model.</param>
		public NfcOpenLinkDecisionPopup(NfcOpenLinkDecisionPopupViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}
	}
}
