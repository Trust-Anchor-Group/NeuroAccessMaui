using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.QR
{
	public partial class ShowQRPopup
	{
		public ShowQRPopup(byte[] QrCodeBin)
		{
			BasePopupViewModel viewModel = new ShowQRViewModel(QrCodeBin);

			this.InitializeComponent();
			this.BindingContext = viewModel;
		}

		public ShowQRPopup()
		{
			this.InitializeComponent();
		}
	}
}
