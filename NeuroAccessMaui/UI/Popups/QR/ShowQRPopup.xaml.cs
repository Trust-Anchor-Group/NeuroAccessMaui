using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.QR
{
	public partial class ShowQRPopup
	{
		public ShowQRPopup(byte[] QrCodeBin, string? QrCodeUri)
		{
			BasePopupViewModel ViewModel = new ShowQRViewModel(QrCodeBin, QrCodeUri);

			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}

		public ShowQRPopup()
		{
			this.InitializeComponent();
		}
	}
}
