namespace NeuroAccessMaui.UI.Popups.Nfc
{
	/// <summary>
	/// Popup used to configure Safe Scan and trusted domains for NFC.
	/// </summary>
	public partial class NfcSafetySettingsPopup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcSafetySettingsPopup"/> class.
		/// </summary>
		/// <param name="ViewModel">The NFC view model to bind against.</param>
		public NfcSafetySettingsPopup(NeuroAccessMaui.UI.Pages.Main.Nfc.NfcViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}
	}
}
