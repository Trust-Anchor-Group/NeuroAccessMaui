using Waher.Events;
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
		/// Tries to get a legal identity. No petition is sent when the identity is protected; use <see cref="RequestIdentityAsync"/> for that.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity to get.</param>
		/// <param name="Purpose">Not used. Kept for existing callers.</param>
		/// <returns>Legal Identity, if possible to get, null otherwise.</returns>
		Task<LegalIdentity?> TryGetLegalIdentity(string LegalId, string Purpose);

		/// <summary>
		/// Opens a contract. Calls <see cref="OpenContractAsync(CaseInsensitiveString, ContractOpenOptions?)"/>.
		/// </summary>
		/// <param name="ContractId">The id of the contract to show.</param>
		/// <param name="Purpose">Not used. Access is only requested after the user confirms.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		Task OpenContract(string ContractId, string Purpose, Dictionary<CaseInsensitiveString, object>? ParameterValues);

		/// <summary>
		/// Opens a contract that has already been downloaded. Calls <see cref="OpenContractAsync(Contract, ContractOpenOptions?)"/>.
		/// </summary>
		/// <param name="Contract">The downloaded contract to show.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		/// <returns>A task representing opening the contract.</returns>
		Task OpenContract(Contract Contract, Dictionary<CaseInsensitiveString, object>? ParameterValues);

		/// <summary>
		/// Asks the user whether to request access to a contract they can't read. Calls <see cref="RequestContractAccessAsync"/>.
		/// </summary>
		/// <param name="ContractId">The id of the contract.</param>
		/// <param name="Purpose">Not used. The user enters the purpose.</param>
		/// <returns>A task representing the request.</returns>
		Task PetitionContract(string ContractId, string Purpose);

		/// <summary>
		/// Opens a contract. A saved copy is shown at once and refreshed in the background. Templates that can be used open
		/// in the new-contract wizard, and other contracts on the contract page.
		/// </summary>
		/// <param name="ContractId">The id of the contract.</param>
		/// <param name="Options">Options, or null.</param>
		/// <returns>If a page opened. When none did, the user has been told why.</returns>
		Task<bool> OpenContractAsync(CaseInsensitiveString ContractId, ContractOpenOptions? Options);

		/// <summary>
		/// Opens a contract that has already been downloaded.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="Options">Options, or null.</param>
		/// <returns>A task that completes when the page has opened.</returns>
		Task OpenContractAsync(Contract Contract, ContractOpenOptions? Options);

		/// <summary>
		/// Declines an invitation to sign a contract: removes the stored contract and its notifications.
		/// </summary>
		/// <param name="ContractId">The id of the contract.</param>
		Task DeclineInvitationAsync(CaseInsensitiveString ContractId);

		/// <summary>
		/// Asks the user for a purpose, and sends a request to see a contract to the people who signed it.
		/// </summary>
		/// <param name="ContractId">The id of the contract.</param>
		/// <returns>If the request was sent.</returns>
		Task<bool> RequestContractAccessAsync(CaseInsensitiveString ContractId);

		/// <summary>
		/// Answers a request from another user to see one of the user's contracts, and marks its notification as handled.
		/// </summary>
		/// <param name="Request">The request.</param>
		/// <param name="Accept">If the request is accepted.</param>
		Task RespondToContractPetitionAsync(ContractPetitionRequest Request, bool Accept);

		/// <summary>
		/// Sends a request to see a legal identity, so a contract page can show the part when the owner answers.
		/// </summary>
		/// <param name="LegalId">The legal ID to request.</param>
		/// <param name="Purpose">The purpose shown to the owner.</param>
		/// <returns>The petition ID, or null if the request couldn't be sent. The user has then been told why.</returns>
		Task<string?> RequestIdentityAsync(CaseInsensitiveString LegalId, string Purpose);

		/// <summary>
		/// Checks if a request sent with <see cref="RequestIdentityAsync"/> is still waiting for the owner's answer.
		/// </summary>
		/// <param name="LegalId">The requested legal ID.</param>
		/// <returns>If a request for the legal ID is waiting for an answer.</returns>
		bool IsIdentityRequestPending(CaseInsensitiveString LegalId);

		/// <summary>
		/// Raised when the owner answers a request sent with <see cref="RequestIdentityAsync"/>.
		/// </summary>
		event EventHandlerAsync<IdentityPetitionAnsweredEventArgs>? IdentityPetitionAnswered;

		/// <summary>
		/// TAG Signature request scanned.
		/// </summary>
		/// <param name="Request">Request string.</param>
		Task TagSignature(string Request);
	}
}
