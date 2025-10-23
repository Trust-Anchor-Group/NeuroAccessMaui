using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;
using NeuroAccessMaui.UI.Popups.QR;

namespace NeuroAccessMaui.UI.Popups.Transaction
{
	public partial class PaymentSuccessPopup
	{
		public PaymentSuccessPopup(EDaler.Transaction Transaction, string? Message)
		{
			BasePopupViewModel ViewModel = new PaymentSuccessPopupViewModel(Transaction, Message);

			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}

		public PaymentSuccessPopup(EDaler.Transaction Transaction)
		{
			BasePopupViewModel ViewModel = new PaymentSuccessPopupViewModel(Transaction, null);

			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}

		public PaymentSuccessPopup()
		{
			this.InitializeComponent();
		}
	}
}
