using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using NeuroAccessMaui.UI.Popups;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts.InviteParts
{
	/// <summary>
	/// View model for the sheet that invites the parts of a contract to sign it, with one message for everyone.
	/// Access to the contract is only granted when the user sends. The result is the number of invitations sent.
	/// </summary>
	public partial class InvitePartsViewModel : ReturningPopupViewModel<int>
	{
		private readonly Contract contract;
		private int sentCount;

		/// <summary>
		/// Creates a new instance of the <see cref="InvitePartsViewModel"/> class.
		/// </summary>
		/// <param name="Contract">The contract to invite people to sign.</param>
		/// <param name="Recipients">The people who can be invited.</param>
		public InvitePartsViewModel(Contract Contract, IEnumerable<InviteRecipient> Recipients)
		{
			this.contract = Contract;
			this.message = ServiceRef.Localizer[nameof(AppResources.ProposalDefaultMessage)];

			foreach (InviteRecipient Recipient in Recipients)
			{
				Recipient.PropertyChanged += this.Recipient_PropertyChanged;
				this.Recipients.Add(Recipient);
			}
		}

		/// <summary>
		/// The people who can be invited.
		/// </summary>
		public ObservableCollection<InviteRecipient> Recipients { get; } = [];

		/// <summary>
		/// Gets the parts of a contract who can be invited: everyone but the user who hasn't signed, and whose address is known.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="SuppressedLegalIds">Legal IDs that must not be invited, or null.</param>
		/// <param name="ExcludeTrustProvider">If parts in the trust provider's role are left out.</param>
		/// <param name="Unreachable">If not null, gets the parts who can't be invited yet, because their address isn't known.
		/// That is the case when the user can't see their identity.</param>
		/// <returns>The people who can be invited.</returns>
		public static async Task<IReadOnlyList<InviteRecipient>> GetRecipientsAsync(ObservableContract Contract,
			CaseInsensitiveString[]? SuppressedLegalIds, bool ExcludeTrustProvider, List<ObservablePart>? Unreachable = null)
		{
			List<InviteRecipient> Result = [];

			foreach (ObservableRole Role in Contract.Roles.ToArray())
			{
				foreach (ObservablePart Part in Role.Parts.ToArray())
				{
					if (Part.IsMe || Part.HasSigned)
						continue;

					if (ExcludeTrustProvider && Part.Role == "TrustProvider")
						continue;

					if (SuppressedLegalIds is not null && Array.IndexOf<CaseInsensitiveString>(SuppressedLegalIds, Part.LegalId) >= 0)
						continue;

					if (Result.Any(Recipient => Recipient.LegalId == Part.LegalId && Recipient.Role == Part.Role))
						continue;

					ContactInfo? Info = await ContactInfo.FindByLegalId(Part.LegalId);
					string? BareJid = Info?.BareJid;
					if (string.IsNullOrWhiteSpace(BareJid))
						BareJid = Part.GetJid();

					if (string.IsNullOrWhiteSpace(BareJid))
					{
						Unreachable?.Add(Part);
						continue;
					}

					string? FriendlyName = Info?.FriendlyName;
					if (string.IsNullOrWhiteSpace(FriendlyName))
						FriendlyName = Part.FriendlyName;
					if (string.IsNullOrWhiteSpace(FriendlyName))
						FriendlyName = BareJid;

					Result.Add(new InviteRecipient(Part.LegalId, Part.Role, BareJid, FriendlyName));
				}
			}

			return Result;
		}

		/// <summary>
		/// Shows the sheet, and waits until the user closes it.
		/// </summary>
		/// <param name="Contract">The contract to invite people to sign.</param>
		/// <param name="Recipients">The people who can be invited.</param>
		/// <returns>The number of invitations sent.</returns>
		public static async Task<int> ShowAsync(Contract Contract, IEnumerable<InviteRecipient> Recipients)
		{
			InvitePartsViewModel ViewModel = new(Contract, Recipients);
			return await ServiceRef.PopupService.PushAsync<InvitePartsPopup, InvitePartsViewModel, int>(ViewModel);
		}

		/// <summary>
		/// The message sent to everyone.
		/// </summary>
		[ObservableProperty]
		private string message;

		/// <summary>
		/// If invitations are being sent.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCommand))]
		[NotifyPropertyChangedFor(nameof(CanSend))]
		private bool isSending;

		/// <summary>
		/// If the user has sent, so the sheet can be closed with Done.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasNotSent))]
		private bool hasSent;

		/// <summary>
		/// If the user hasn't sent yet.
		/// </summary>
		public bool HasNotSent => !this.HasSent;

		/// <summary>
		/// If there is anyone selected who hasn't been invited yet.
		/// </summary>
		public bool CanSend => !this.IsSending && this.Recipients.Any(Recipient => Recipient.IsSelected && !Recipient.IsSent);

		private void Recipient_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(InviteRecipient.IsSelected) || e.PropertyName == nameof(InviteRecipient.IsSent))
			{
				this.OnPropertyChanged(nameof(this.CanSend));
				this.SendCommand.NotifyCanExecuteChanged();
			}
		}

		/// <summary>
		/// Grants each selected person access to the contract and sends them the invitation. Each person shows whether it
		/// was sent, or why not.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanSend))]
		private async Task SendAsync()
		{
			this.IsSending = true;

			try
			{
				string Text = string.IsNullOrWhiteSpace(this.Message)
					? ServiceRef.Localizer[nameof(AppResources.ProposalDefaultMessage)]
					: this.Message.Trim();

				foreach (InviteRecipient Recipient in this.Recipients.ToArray())
				{
					if (!Recipient.IsSelected || Recipient.IsSent)
						continue;

					string? Error = await this.SendToAsync(Recipient, Text);

					Recipient.HasFailed = Error is not null;
					Recipient.StatusText = Error ?? ServiceRef.Localizer[nameof(AppResources.InviteStatusSent)];

					if (Error is null)
					{
						Recipient.IsSent = true;
						this.sentCount++;
					}
				}
			}
			finally
			{
				this.IsSending = false;
				this.HasSent = true;
			}
		}

		/// <summary>
		/// Sends one invitation.
		/// </summary>
		/// <returns>null if it was sent, otherwise why it wasn't.</returns>
		private async Task<string?> SendToAsync(InviteRecipient Recipient, string Text)
		{
			try
			{
				await ServiceRef.XmppService.ContractsClient.AuthorizeAccessToContractAsync(this.contract.ContractId, Recipient.BareJid, true);
				await ServiceRef.XmppService.SendContractProposal(this.contract, Recipient.Role, Recipient.BareJid, Text);
				return null;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return GetFailureText(Ex);
			}
		}

		/// <summary>
		/// Explains why an invitation couldn't be sent. Contracts with encrypted values can only be sent end-to-end encrypted,
		/// to a contact who is online.
		/// </summary>
		private static string GetFailureText(Exception Ex)
		{
			string Text = Ex.Message ?? string.Empty;

			if (Ex is InvalidOperationException && Text.StartsWith("End-to-End encryption not enabled", StringComparison.OrdinalIgnoreCase))
				return ServiceRef.Localizer[nameof(AppResources.InviteStatusNoEncryption)];

			if (Ex is ArgumentException)
			{
				if (Text.StartsWith("Recipient not in roster", StringComparison.OrdinalIgnoreCase))
					return ServiceRef.Localizer[nameof(AppResources.InviteStatusNotMutualContact)];

				if (Text.StartsWith("Recipient not online", StringComparison.OrdinalIgnoreCase))
					return ServiceRef.Localizer[nameof(AppResources.InviteStatusOffline)];
			}

			return ContractErrorMessages.GetMessage(Ex);
		}

		/// <summary>
		/// Closes the sheet. Nothing more is sent.
		/// </summary>
		[RelayCommand]
		private async Task CloseAsync()
		{
			this.TrySetResult(this.sentCount);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <inheritdoc/>
		protected override Task OnPopAsync()
		{
			foreach (InviteRecipient Recipient in this.Recipients)
				Recipient.PropertyChanged -= this.Recipient_PropertyChanged;

			this.TrySetResult(this.sentCount);
			return base.OnPopAsync();
		}
	}
}
