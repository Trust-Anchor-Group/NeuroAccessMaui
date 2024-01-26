using NeuroAccessMaui.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionContract
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a contract petition request.
	/// </summary>
	public class PetitionContractNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="PetitionContractNavigationArgs"/> class.
		/// </summary>
		public PetitionContractNavigationArgs() { }

		/// <summary>
		/// Creates an instance of the <see cref="PetitionContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="requestorIdentity">The identity of the requestor</param>
		/// <param name="requestorFullJid">The identity of the requestor</param>
		/// <param name="requestedContract">The identity of the requestor</param>
		/// <param name="petitionId">The petition id.</param>
		/// <param name="purpose">The purpose of the petition.</param>
		public PetitionContractNavigationArgs(
				LegalIdentity requestorIdentity,
				string requestorFullJid,
				Contract requestedContract,
				string petitionId,
				string purpose)
		{
			this.RequestorIdentity = requestorIdentity;
			this.RequestorFullJid = requestorFullJid;
			this.RequestedContract = requestedContract;
			this.PetitionId = petitionId;
			this.Purpose = purpose;
		}

		/// <summary>
		/// The identity of the requestor.
		/// </summary>
		public LegalIdentity? RequestorIdentity { get; }
		/// <summary>
		/// The identity of the requestor.
		/// </summary>
		public string? RequestorFullJid { get; }
		/// <summary>
		/// The identity of the requestor.
		/// </summary>
		public Contract? RequestedContract { get; }
		/// <summary>
		/// The petition id.
		/// </summary>
		public string? PetitionId { get; }
		/// <summary>
		/// The purpose of the petition.
		/// </summary>
		public string? Purpose { get; }
	}
}
