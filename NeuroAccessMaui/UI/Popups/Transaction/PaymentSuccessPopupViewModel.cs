using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;

namespace NeuroAccessMaui.UI.Popups.Transaction
{
	public partial class PaymentSuccessPopupViewModel(TransactionEventItem Event) : BasePopupViewModel
	{
		[ObservableProperty]
		private TransactionEventItem transactionEventItem = Event;

		[ObservableProperty]
		private bool isContact = Event.IsContact;

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
