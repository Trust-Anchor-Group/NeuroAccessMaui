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
using NeuroAccessMaui.UI.Pages.Contacts.Chat.Session;
using NeuroAccessMaui.Services.Contacts; // Added for ContactInfo lookup
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;
using System.Globalization; // Added for identity navigation

namespace NeuroAccessMaui.UI.Popups.Transaction
{
	public partial class TransactionPopupViewModel(TransactionEventItem Event) : BasePopupViewModel
	{
		[ObservableProperty]
		private TransactionEventItem transactionEevent = Event;

		[ObservableProperty]
		private bool isContact;

		public decimal Change => this.TransactionEevent.Change < 0 ? -this.TransactionEevent.Change : this.TransactionEevent.Change;

		public bool IsSender => this.TransactionEevent.Change < 0;

		public bool HasMessage => this.TransactionEevent.HasMessage;

		public bool MessageIsUrl => this.HasMessage && Uri.IsWellFormedUriString(this.TransactionEevent.Message, UriKind.Absolute);

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			await this.EvaluateIsContact();

			this.OnPropertyChanged(nameof(this.IsContact));
		}

		public string? Message
		{
			get
			{
				if (this.HasMessage)
					return this.TransactionEevent.Message;
				else
					return null;
			}
		}

		public string DestinationString
		{
			get
			{
				if (this.IsSender)
					return ServiceRef.Localizer[nameof(AppResources.To)];
				else
					return ServiceRef.Localizer[nameof(AppResources.From)];
			}
		}

		#region Commands

		[RelayCommand]
		private async Task Close()
		{
			await ServiceRef.PopupService.PopAsync();
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
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args);
				await ServiceRef.PopupService.PopAsync();
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

				ContactInfo? Contact = await ContactInfo.FindByBareJid(Remote);
				Contact ??= await ContactInfo.FindByLegalId(Remote);

				if (Contact is null)
					return;

				LegalIdentity? Identity = Contact.LegalIdentity;
				if (Identity is not null)
				{
					ViewIdentityNavigationArgs ViewIdentityArgs = new(Identity);
					await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), ViewIdentityArgs);
					await ServiceRef.PopupService.PopAsync();
				}
				else
				{
					string LegalId = Contact.LegalId;
					if (!string.IsNullOrEmpty(LegalId))
					{
						await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(LegalId, ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);
						await ServiceRef.PopupService.PopAsync();
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		protected override Task OnPopAsync()
		{
			return base.OnPopAsync();
		}

		[RelayCommand]
		private async Task OpenMessageUri()
		{
			if (this.MessageIsUrl && this.Message is not null)
			{
				try
				{
					await App.OpenUrlAsync(this.Message);
					await ServiceRef.PopupService.PopAsync();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}
		}
		#endregion

		private async Task EvaluateIsContact()
		{
			string Remote = this.TransactionEevent.Remote;
			if (string.IsNullOrEmpty(Remote))
			{
				this.IsContact = false;
				return;
			}

			// Count as contact if self
			if (string.Equals(Remote, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase) || string.Equals(Remote, ServiceRef.TagProfile.LegalJid, StringComparison.OrdinalIgnoreCase))
			{
				this.IsContact = true;
				return;
			}

			try
			{
				ContactInfo? Contact2 = await ContactInfo.FindByLegalId(Remote);
				if (Contact2 is not null)
				{
					this.IsContact = true;
					return;
				}

				ContactInfo Contact = await ContactInfo.FindByBareJid(Remote);
				if (Contact is not null)
				{
					this.IsContact = true;
					return;
				}

				RosterItem? Item = ServiceRef.XmppService.GetRosterItem(Remote);
				if (Item is not null)
				{
					this.IsContact = true;
					return;
				}
			}
			catch
			{
				// Do nothing
			}
		}
	}
}
