using System;
using NeuroAccessMaui.Services.Nfc.Ui;

namespace NeuroAccessMaui.UI.Popups.Nfc
{
	/// <summary>
	/// Popup that shows details for a single decoded NDEF record.
	/// </summary>
	public partial class NfcNdefRecordDetailsPopup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcNdefRecordDetailsPopup"/> class.
		/// </summary>
		/// <param name="Record">Record to display.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="Record"/> is null.</exception>
		public NfcNdefRecordDetailsPopup(NfcNdefRecordSnapshot Record)
		{
			if (Record is null)
				throw new ArgumentNullException(nameof(Record));

			this.InitializeComponent();
			this.BindingContext = new NfcNdefRecordDetailsPopupViewModel(Record);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NfcNdefRecordDetailsPopup"/> class.
		/// </summary>
		public NfcNdefRecordDetailsPopup()
		{
			this.InitializeComponent();
		}
	}
}
