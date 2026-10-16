using System.ComponentModel;
using System.Text;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI.Converters;
using Waher.Content.Markdown;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.BlockElements;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a row in the list of contracts or templates.
	/// </summary>
	public class ContractModel : IUniqueItem, INotifyPropertyChanged
	{
		private readonly string contractId;
		private ContractReference contractRef;

		/// <summary>
		/// The data model for a row in the list of contracts or templates.
		/// </summary>
		/// <param name="ContractRef">The stored contract.</param>
		public ContractModel(ContractReference ContractRef)
		{
			this.contractRef = ContractRef;
			this.contractId = ContractRef.ContractId?.Value ?? string.Empty;
		}

		/// <summary>
		/// Gets a displayable name for a contract.
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <returns>Displayable Name</returns>
		public static async Task<string> GetName(Contract? Contract)
		{
			if (Contract?.Parts is null)
				return string.Empty;

			Dictionary<string, ClientSignature> Signatures = [];
			string? MyLegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
			StringBuilder? StringBuilder = null;

			if (Contract.ClientSignatures is not null)
			{
				foreach (ClientSignature Signature in Contract.ClientSignatures)
					Signatures[Signature.LegalId] = Signature;
			}

			foreach (Part Part in Contract.Parts)
			{
				if (string.Equals(Part.LegalId, MyLegalId, StringComparison.OrdinalIgnoreCase) ||
					 (Signatures.TryGetValue(Part.LegalId, out ClientSignature? PartSignature) &&
					 string.Equals(PartSignature.BareJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase)))
				{
					continue;   // Self
				}

				string FriendlyName = await ContactInfo.GetFriendlyName(Part.LegalId);

				if (StringBuilder is null)
					StringBuilder = new StringBuilder(FriendlyName);
				else
				{
					StringBuilder.Append(", ");
					StringBuilder.Append(FriendlyName);
				}
			}

			return StringBuilder?.ToString() ?? string.Empty;
		}

		/// <summary>
		/// Gets the category of a contract
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <returns>Contract Category</returns>
		public static async Task<string?> GetCategory(Contract Contract)
		{
			HumanReadableText[] Localizations = Contract.ForHumans;
			string Language = Contract.DeviceLanguage();

			foreach (HumanReadableText Localization in Localizations)
			{
				if (!string.Equals(Localization.Language, Language, StringComparison.OrdinalIgnoreCase))
					continue;

				foreach (BlockElement Block in Localization.Body)
				{
					if (Block is Section Section)
					{
						MarkdownOutput Markdown = new();

						foreach (InlineElement Item in Section.Header)
							await Item.GenerateMarkdown(Markdown, 1, 0, new Waher.Networking.XMPP.Contracts.HumanReadable.MarkdownSettings(Contract, MarkdownType.ForRendering));

						MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown.ToString());

						return (await Doc.GeneratePlainText()).Trim();
					}
				}
			}

			return null;
		}

		/// <summary>
		/// The contract id.
		/// </summary>
		public string ContractId => this.contractId;

		/// <inheritdoc/>
		public string UniqueName => this.ContractId;

		/// <summary>
		/// URI string for the contract ID, used for sharing and QR codes.
		/// </summary>
		public string ContractIdUriString => Constants.UriSchemes.IotSc + ":" + this.contractId;

		/// <summary>
		/// The created timestamp of the contract.
		/// </summary>
		public DateTime Timestamp => this.contractRef.Created;

		/// <summary>
		/// A reference to the contract.
		/// </summary>
		public ContractReference ContractRef => this.contractRef;

		/// <summary>
		/// Names of the other parties of the contract.
		/// </summary>
		public string Name => this.contractRef.Name ?? string.Empty;

		/// <summary>
		/// Name, or category if no name.
		/// </summary>
		public string NameOrCategory => string.IsNullOrEmpty(this.Name) ? this.Category : this.Name;

		/// <summary>
		/// Displayable category for the contract. This is the header of its first section, which works as its title.
		/// </summary>
		public string Category => this.contractRef.Category ?? string.Empty;

		/// <summary>
		/// The name shown for the contract: its title, else a text for an invitation that can't be read yet, else its ID.
		/// </summary>
		public string DisplayName
		{
			get
			{
				if (!string.IsNullOrEmpty(this.contractRef.Category))
					return this.contractRef.Category;

				if (!this.contractRef.ContractLoaded && !string.IsNullOrEmpty(this.contractRef.InvitedBy))
					return ServiceRef.Localizer[nameof(AppResources.NotificationInvitationTitleUnnamed)];

				return this.contractId;
			}
		}

		/// <summary>
		/// Names of the other parties. The user's own name is never included.
		/// </summary>
		public string PartyNames => this.contractRef.PartyNames ?? string.Empty;

		/// <summary>
		/// If there are names of other parties to show.
		/// </summary>
		public bool HasPartyNames => !string.IsNullOrEmpty(this.contractRef.PartyNames);

		/// <summary>
		/// The group the contract is listed under.
		/// </summary>
		public ContractListGroup Group => this.contractRef.ListGroup;

		/// <summary>
		/// The localized state of the contract, or an empty string if the contract hasn't been read.
		/// </summary>
		public string StateText
		{
			get
			{
				if (this.contractRef.IsDeletedOnServer)
					return ContractStateToString.GetText(ContractState.Deleted, false);

				if (!this.contractRef.ContractLoaded)
					return string.Empty;

				return ContractStateToString.GetText(this.contractRef.State, this.contractRef.IsTemplate);
			}
		}

		/// <summary>
		/// If there is a state to show.
		/// </summary>
		public bool HasStateText => !string.IsNullOrEmpty(this.StateText);

		/// <summary>
		/// The user's roles in the contract, or the role the user was invited to, as "Your role: X".
		/// </summary>
		public string MyRoleText
		{
			get
			{
				string[]? Roles = this.contractRef.MyRoles;
				string? Text = Roles is not null && Roles.Length > 0 ? string.Join(", ", Roles) : this.contractRef.InvitedRole;

				if (string.IsNullOrEmpty(Text))
					return string.Empty;

				return ServiceRef.Localizer[nameof(AppResources.ContractRowRoleFormat), Text];
			}
		}

		/// <summary>
		/// If the user has, or is invited to, a role.
		/// </summary>
		public bool HasMyRole => !string.IsNullOrEmpty(this.MyRoleText);

		/// <summary>
		/// When the contract last changed, as a short local text: a time today, "Yesterday", or a date.
		/// </summary>
		public string LastChangedText
		{
			get
			{
				DateTime Changed = this.contractRef.LastChanged;
				if (Changed == DateTime.MinValue)
					Changed = this.contractRef.Created;

				if (Changed == DateTime.MinValue)
					return string.Empty;

				DateTime Local = Changed.Kind switch
				{
					DateTimeKind.Local => Changed,
					DateTimeKind.Utc => Changed.ToLocalTime(),
					_ => DateTime.SpecifyKind(Changed, DateTimeKind.Utc).ToLocalTime()
				};

				DateTime Today = DateTime.Today;

				if (Local.Date == Today)
					return Local.ToShortTimeString();

				if (Local.Date == Today.AddDays(-1))
					return ServiceRef.Localizer[nameof(AppResources.Yesterday)];

				return Local.ToShortDateString();
			}
		}

		/// <summary>
		/// If the contract changed since the user last opened it.
		/// </summary>
		public bool IsUnread => this.contractRef.IsUnread;

		/// <summary>
		/// A short marker saying what the user needs to do: "Invitation" or "Needs your signature".
		/// </summary>
		public string MarkerText
		{
			get
			{
				if (this.contractRef.HasOpenInvitation)
					return ServiceRef.Localizer[nameof(AppResources.ContractMarkerInvitation)];

				if (this.contractRef.ListGroup == ContractListGroup.NeedsAction)
					return ServiceRef.Localizer[nameof(AppResources.ContractMarkerNeedsSignature)];

				return string.Empty;
			}
		}

		/// <summary>
		/// If there is a marker to show.
		/// </summary>
		public bool HasMarker => !string.IsNullOrEmpty(this.MarkerText);

		/// <summary>
		/// Replaces the stored contract the row shows, and updates every displayed value.
		/// </summary>
		/// <param name="ContractRef">The updated stored contract.</param>
		public void Update(ContractReference ContractRef)
		{
			this.contractRef = ContractRef;
			this.OnPropertyChanged(string.Empty);
		}

		/// <summary>
		/// Called when a property has changed.
		/// </summary>
		/// <param name="PropertyName">Name of property</param>
		public void OnPropertyChanged(string PropertyName)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
