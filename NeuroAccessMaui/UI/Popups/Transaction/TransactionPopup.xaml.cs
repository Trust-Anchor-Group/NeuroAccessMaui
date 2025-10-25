using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;
using NeuroAccessMaui.UI.Popups.QR;

namespace NeuroAccessMaui.UI.Popups.Transaction
{
	public partial class TransactionPopup
	{
		public TransactionPopup(TransactionEventItem Event)
		{
			BasePopupViewModel ViewModel = new TransactionPopupViewModel(Event);

			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}

		public TransactionPopup()
		{
			this.InitializeComponent();
		}
	}
}
