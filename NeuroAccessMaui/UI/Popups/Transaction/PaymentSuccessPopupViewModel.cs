using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;

namespace NeuroAccessMaui.UI.Popups.Transaction
{
	public partial class PaymentSuccessPopupViewModel(EDaler.Transaction Transaction, string? Message) : BasePopupViewModel
	{
		[ObservableProperty]
		private EDaler.Transaction transaction = Transaction;

		private readonly string? message = Message;

		public string Amount => this.Transaction.Amount + " " + this.Transaction.Currency;

		public string Message
		{
			get => this.message ?? "";
		}

		#region Commands

		[RelayCommand]
		private async Task Close()
		{
			await ServiceRef.UiService.PopAsync();
		}

		public override Task OnPop()
		{
			return base.OnPop();
		}

		#endregion
	}
}
