using System;
using NeuroAccessMaui.UI.Pages.Main.Nfc;

namespace NeuroAccessMaui.UI.Popups.Nfc
{
	/// <summary>
	/// Popup that shows details for a single NFC scan history entry.
	/// </summary>
	public partial class NfcHistoryDetailsPopup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcHistoryDetailsPopup"/> class.
		/// </summary>
		/// <param name="Item">History item to show.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="Item"/> is null.</exception>
		public NfcHistoryDetailsPopup(NfcScanHistoryListItem Item)
		{
			if (Item is null)
				throw new ArgumentNullException(nameof(Item));

			this.InitializeComponent();
			this.BindingContext = new NfcHistoryDetailsPopupViewModel(Item);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NfcHistoryDetailsPopup"/> class.
		/// </summary>
		public NfcHistoryDetailsPopup()
		{
			this.InitializeComponent();
		}
	}
}
