using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Contracts;

/// <summary>
/// Orchestrates operations on contracts upon receiving certain events, like approving or rejecting other peers' review requests.
/// Also keeps track of the <see cref="ITagProfile"/> for the current user, and applies the correct navigation should the legal identity be compromised or revoked.
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
	/// TAG Signature request scanned.
	/// </summary>
	/// <param name="Request">Request string.</param>
	Task TagSignature(string Request);
}
