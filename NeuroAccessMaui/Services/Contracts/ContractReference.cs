using System.Text;
using System.Xml;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroFeatures;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Contains a local reference to a contract that the user has created or signed.
	/// </summary>
	[CollectionName("ContractReferences")]
	[Index("ContractId")]
	[Index("IsTemplate", "ContractLoaded", "Category", "Name", "Created")]
	[Index("IsTemplate", "ListGroup", "-LastChanged")]
	public class ContractReference
	{
		private Contract? contract;

		/// <summary>
		/// Contains a local reference to a contract that the user has created or signed.
		/// </summary>
		public ContractReference()
		{
		}

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// Contract reference
		/// </summary>
		public CaseInsensitiveString? ContractId { get; set; }

		/// <summary>
		/// When object was created
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// When object was last updated.
		/// </summary>
		public DateTime Updated { get; set; }

		/// <summary>
		/// When object was last loaded.
		/// </summary>
		public DateTime Loaded { get; set; }

		/// <summary>
		/// XML of contract.
		/// </summary>
		public string? ContractXml { get; set; }

		/// <summary>
		/// Name of contract
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Category of contract
		/// </summary>
		public string? Category { get; set; }

		/// <summary>
		/// If a local copy of the contract is available.
		/// </summary>
		public bool ContractLoaded { get; set; }

		/// <summary>
		/// If the contract can work as a template
		/// </summary>
		public bool IsTemplate { get; set; }

		/// <summary>
		/// If the contract represents a token creation template
		/// </summary>
		[DefaultValue(false)]
		public bool IsTokenCreationTemplate { get; set; }

		/// <summary>
		/// Contract state
		/// </summary>
		public ContractState State { get; set; }

		/// <summary>
		/// If the user's account created the contract.
		/// </summary>
		public bool IsCreator { get; set; }

		/// <summary>
		/// The user's legal IDs that appear as parts or signatures in the contract.
		/// </summary>
		public string[]? MyLegalIds { get; set; }

		/// <summary>
		/// The roles the user is listed in, or has signed.
		/// </summary>
		public string[]? MyRoles { get; set; }

		/// <summary>
		/// If the user has signed the contract.
		/// </summary>
		public bool HasSigned { get; set; }

		/// <summary>
		/// Bare JID of the sender of an invitation to sign the contract.
		/// </summary>
		public string? InvitedBy { get; set; }

		/// <summary>
		/// The role proposed in the invitation.
		/// </summary>
		public string? InvitedRole { get; set; }

		/// <summary>
		/// The message sent with the invitation. Plain text.
		/// </summary>
		public string? InvitationMessage { get; set; }

		/// <summary>
		/// When the invitation arrived (UTC).
		/// </summary>
		public DateTime? InvitedAt { get; set; }

		/// <summary>
		/// The group the contract is listed under in My contracts.
		/// </summary>
		public ContractListGroup ListGroup { get; set; } = ContractListGroup.InProgress;

		/// <summary>
		/// The latest of the contract's update, its last signature and the invitation (UTC).
		/// </summary>
		public DateTime LastChanged { get; set; }

		/// <summary>
		/// When the user last opened the contract (UTC).
		/// </summary>
		public DateTime LastViewed { get; set; }

		/// <summary>
		/// Display names of the other parties.
		/// </summary>
		public string? PartyNames { get; set; }

		/// <summary>
		/// Lower-case text that the list searches: name, category, party names and contract ID.
		/// </summary>
		public string? SearchText { get; set; }

		/// <summary>
		/// If new contracts can be created from the contract.
		/// </summary>
		public bool CanActAsTemplate { get; set; }

		/// <summary>
		/// If the contract was deleted on the server, and this row is a read-only copy.
		/// </summary>
		public bool IsDeletedOnServer { get; set; }

		/// <summary>
		/// When signing closes (UTC), if the contract has a signing window.
		/// </summary>
		public DateTime? SignBefore { get; set; }

		/// <summary>
		/// If the contract changed since the user last opened it.
		/// </summary>
		[IgnoreMember]
		public bool IsUnread => this.LastViewed < this.LastChanged;

		/// <summary>
		/// If the row holds an invitation the user hasn't signed or declined.
		/// </summary>
		[IgnoreMember]
		public bool HasOpenInvitation =>
			!string.IsNullOrEmpty(this.InvitedBy) &&
			!this.HasSigned &&
			!this.IsDeletedOnServer &&
			this.State is not (ContractState.Obsoleted or ContractState.Deleted or ContractState.Failed or ContractState.Rejected);

		/// <summary>
		/// Gets a parsed contract.
		/// </summary>
		/// <returns>Parsed contract</returns>
		public async Task<Contract?> GetContract()
		{
			if (this.contract is null && !string.IsNullOrEmpty(this.ContractXml))
			{
				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};
				Doc.LoadXml(this.ContractXml);

				ParsedContract Parsed = await Contract.Parse(Doc.DocumentElement, ServiceRef.XmppService.ContractsClient, false);

				this.contract = Parsed?.Contract;
			}

			return this.contract;
		}

		/// <summary>
		/// Sets a parsed contract.
		/// </summary>
		/// <param name="Contract">Contract</param>
		public async Task SetContract(Contract Contract)
		{
			this.contract = Contract;

			if (Contract is null)
			{
				this.ContractXml = null;
				this.ContractLoaded = false;
				this.IsTemplate = false;
				this.IsTokenCreationTemplate = false;
				this.Name = string.Empty;
				this.Category = string.Empty;
				this.State = ContractState.Failed;
			}
			else
			{
				StringBuilder Xml = new();
				Contract.Serialize(Xml, true, true, true, true, true, true, true);
				this.ContractXml = Xml.ToString();
				this.ContractLoaded = true;
				this.ContractId = Contract.ContractId;
				this.IsTemplate = Contract.PartsMode == ContractParts.TemplateOnly;
				this.State = Contract.State;
				this.Created = Contract.Created;
				this.Updated = Contract.Updated;
				this.Loaded = DateTime.UtcNow;
				this.CanActAsTemplate = Contract.CanActAsTemplate;
				this.SignBefore = Contract.SignBefore;

				this.IsTokenCreationTemplate =
					this.IsTemplate &&
					Contract.ForMachinesLocalName == "Create" &&
					Contract.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures;

				this.Name = await ContractModel.GetName(Contract);
				this.Category = await ContractModel.GetCategory(Contract);
			}
		}
	}
}
