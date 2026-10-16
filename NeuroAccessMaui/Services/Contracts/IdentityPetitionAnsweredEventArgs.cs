using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Event arguments for <see cref="IContractOrchestratorService.IdentityPetitionAnswered"/>.
	/// </summary>
	/// <param name="PetitionId">The ID of the answered petition.</param>
	/// <param name="LegalId">The legal ID that was requested.</param>
	/// <param name="Accepted">If the owner accepted the request.</param>
	/// <param name="Identity">The identity, when the request was accepted.</param>
	public class IdentityPetitionAnsweredEventArgs(string PetitionId, string LegalId, bool Accepted, LegalIdentity? Identity) : EventArgs
	{
		/// <summary>
		/// The ID of the answered petition.
		/// </summary>
		public string PetitionId { get; } = PetitionId;

		/// <summary>
		/// The legal ID that was requested.
		/// </summary>
		public string LegalId { get; } = LegalId;

		/// <summary>
		/// If the owner accepted the request.
		/// </summary>
		public bool Accepted { get; } = Accepted;

		/// <summary>
		/// The identity, when the request was accepted.
		/// </summary>
		public LegalIdentity? Identity { get; } = Identity;
	}
}
