using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Orchestrates operations on contracts upon receiving certain events, like approving or rejecting other peers' review requests.
	/// </summary>
	[DefaultImplementation(typeof(ContractOrchestratorService))]
	public interface IContractOrchestratorService : ILoadableService
	{
		/// <summary>
		/// Downloads the specified <see cref="LegalIdentity"/> and opens the corresponding page in the app to show it.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity to show.</param>
		/// <param name="Purpose">The purpose to state if the identity can't be downloaded and needs to be petitioned instead.</param>
		Task OpenLegalIdentity(string LegalId, string Purpose);

		/// <summary>
		/// Tries to get a legal identity.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity to show.</param>
		/// <param name="Purpose">The purpose to state if the identity can't be downloaded and needs to be petitioned instead.</param>
		/// <returns>Legal Identity, if possible to get, null otherwise.</returns>
		Task<LegalIdentity?> TryGetLegalIdentity(string LegalId, string Purpose);

		/// <summary>
		/// Downloads the specified <see cref="Contract"/> and opens the corresponding page in the app to show it.
		/// </summary>
		/// <param name="ContractId">The id of the contract to show.</param>
		/// <param name="Purpose">The purpose to state if the contract can't be downloaded and needs to be petitioned instead.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		Task OpenContract(
			string ContractId,
			string Purpose,
			Dictionary<CaseInsensitiveString, object>? ParameterValues);

		/// <summary>
		/// Downloads and opens a contract while preserving proposal context.
		/// </summary>
		/// <param name="ContractId">The id of the contract to show.</param>
		/// <param name="Purpose">The purpose to state if the contract can't be downloaded and needs to be petitioned instead.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		/// <param name="Role">The proposed role when opening a contract proposal.</param>
		/// <param name="Proposal">The proposal message, if any.</param>
		/// <param name="FromJid">The sender of the proposal, if any.</param>
		Task OpenContract(
			string ContractId,
			string Purpose,
			Dictionary<CaseInsensitiveString, object>? ParameterValues,
			string? Role,
			string? Proposal,
			string? FromJid);

		/// <summary>
		/// Opens an already-loaded contract through the canonical contract navigation path.
		/// </summary>
		/// <param name="Contract">The contract to open.</param>
		/// <param name="Purpose">The purpose to state if access must be petitioned.</param>
		/// <param name="ParameterValues">Parameter values to set when the contract is a template.</param>
		/// <param name="SourceReference">The existing persisted reference that supplied the contract, if any.</param>
		/// <param name="Role">The proposed role when opening a contract proposal.</param>
		/// <param name="Proposal">The proposal message, if any.</param>
		/// <param name="FromJid">The sender of the proposal, if any.</param>
		Task OpenContract(
			Contract Contract,
			string Purpose,
			Dictionary<CaseInsensitiveString, object>? ParameterValues,
			ContractReference? SourceReference = null,
			string? Role = null,
			string? Proposal = null,
			string? FromJid = null);

		/// <summary>
		/// Opens a saved contract reference before downloading any missing contract details.
		/// </summary>
		/// <param name="SourceReference">The persisted reference to open.</param>
		/// <param name="Role">The proposed role when opening a contract proposal.</param>
		/// <param name="Proposal">The proposal message, if any.</param>
		/// <param name="FromJid">The sender of the proposal, if any.</param>
		/// <returns>A task representing the navigation operation.</returns>
		Task OpenContract(
			ContractReference SourceReference,
			string? Role = null,
			string? Proposal = null,
			string? FromJid = null);

		/// <summary>
		/// TAG Signature request scanned.
		/// </summary>
		/// <param name="Request">Request string.</param>
		Task TagSignature(string Request);
	}
}
