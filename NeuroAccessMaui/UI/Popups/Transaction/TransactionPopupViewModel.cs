using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Waher.Networking.XMPP;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.Services.Contacts; // Added for ContactInfo lookup
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts; // Added for identity navigation

namespace NeuroAccessMaui.UI.Popups.Transaction
{
	public partial class TransactionPopupViewModel(TransactionEventItem Event) : BasePopupViewModel
	{
		[ObservableProperty]
		private TransactionEventItem transactionEevent = Event;

		[ObservableProperty]
		private bool isContact = Event.IsContact;

		#region Commands

		[RelayCommand]
		private async Task Close()
		{
			await ServiceRef.UiService.PopAsync();
		}

		[RelayCommand]
		private async Task OpenChat()
		{
			try
			{
				string Remote = this.TransactionEevent.Remote;
				if (string.IsNullOrEmpty(Remote))
					return;

				string? BareJid = null;
				string? LegalId = null;
				int AtIndex = Remote.IndexOf('@');
				if (AtIndex > 0)
				{
					BareJid = Remote;
					string AccountPart = Remote.Substring(0, AtIndex);
					if (Guid.TryParse(AccountPart, out Guid _))
						LegalId = AccountPart;
				}

				ChatNavigationArgs Args = new(LegalId, BareJid ?? Remote, this.TransactionEevent.FriendlyName);
				await ServiceRef.UiService.GoToAsync(nameof(ChatPage), Args);
				await ServiceRef.UiService.PopAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Views the legal identity of the remote party, using the same strategy as ContactInfoModel.ViewContact.
		/// </summary>
		[RelayCommand]
		private async Task ViewId()
		{
			try
			{
				string Remote = this.TransactionEevent.Remote;
				if (string.IsNullOrEmpty(Remote))
					return;

				ContactInfo Contact = await ContactInfo.FindByBareJid(Remote);
				if (Contact is null)
					return;

				LegalIdentity? Identity = Contact.LegalIdentity;
				if (Identity is not null)
				{
					ViewIdentityNavigationArgs ViewIdentityArgs = new(Identity);
					await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage), ViewIdentityArgs);
				}
				else
				{
					string LegalId = Contact.LegalId;
					if (!string.IsNullOrEmpty(LegalId))
						await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(LegalId, ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		public override Task OnPop()
		{
			return base.OnPop();
		}

		#endregion
	}
}
