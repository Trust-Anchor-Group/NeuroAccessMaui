using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// A request from another user to see one of the user's contracts.
	/// </summary>
	/// <param name="ContractId">The ID of the requested contract.</param>
	/// <param name="PetitionId">The petition ID, used to answer the request.</param>
	/// <param name="RequestorFullJid">Full JID of the requester, used to answer the request.</param>
	/// <param name="RequestorIdentity">The requester's legal identity, if known.</param>
	/// <param name="Purpose">The purpose the requester gave. Plain text.</param>
	public class ContractPetitionRequest(CaseInsensitiveString ContractId, string PetitionId, string RequestorFullJid,
		LegalIdentity? RequestorIdentity, string? Purpose)
	{
		/// <summary>
		/// The ID of the requested contract.
		/// </summary>
		public CaseInsensitiveString ContractId { get; } = ContractId;

		/// <summary>
		/// The petition ID, used to answer the request.
		/// </summary>
		public string PetitionId { get; } = PetitionId;

		/// <summary>
		/// Full JID of the requester, used to answer the request.
		/// </summary>
		public string RequestorFullJid { get; } = RequestorFullJid;

		/// <summary>
		/// The requester's legal identity, if known.
		/// </summary>
		public LegalIdentity? RequestorIdentity { get; } = RequestorIdentity;

		/// <summary>
		/// The purpose the requester gave. Plain text.
		/// </summary>
		public string? Purpose { get; } = Purpose;
	}
}
