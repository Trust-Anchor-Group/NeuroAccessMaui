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
		/// Sends a petition to view a legal identity that the user is not permitted to download, and tells the user it was sent.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity to petition.</param>
		/// <param name="Purpose">The purpose to state in the petition.</param>
		/// <returns>A task representing the petition.</returns>
		Task PetitionLegalIdentity(string LegalId, string Purpose);

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
		Task OpenContract(string ContractId, string Purpose, Dictionary<CaseInsensitiveString, object>? ParameterValues);

		/// <summary>
		/// Opens the page for a <see cref="Contract"/> that has already been downloaded, without downloading it again.
		/// </summary>
		/// <param name="Contract">The downloaded contract to show.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		/// <returns>A task representing opening the contract.</returns>
		Task OpenContract(Contract Contract, Dictionary<CaseInsensitiveString, object>? ParameterValues);

		/// <summary>
		/// Sends a petition to view a contract that the user is not permitted to download, and tells the user it was sent.
		/// </summary>
		/// <param name="ContractId">The id of the contract to petition.</param>
		/// <param name="Purpose">The purpose to state in the petition.</param>
		/// <returns>A task representing the petition.</returns>
		Task PetitionContract(string ContractId, string Purpose);

		/// <summary>
		/// TAG Signature request scanned.
		/// </summary>
		/// <param name="Request">Request string.</param>
		Task TagSignature(string Request);
	}
}
