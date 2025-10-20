using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;

namespace NeuroAccessMaui.UI.Popups.Transaction
{
	public partial class TransactionPopupViewModel(TransactionEventItem Event) : BasePopupViewModel
	{

		public readonly TransactionEventItem Event = Event;


		[ObservableProperty]
		private bool isFriend = true;


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
