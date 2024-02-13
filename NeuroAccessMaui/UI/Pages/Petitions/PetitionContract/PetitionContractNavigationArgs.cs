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
		/// <param name="RequestorIdentity">The identity of the requestor</param>
		/// <param name="RequestorFullJid">The identity of the requestor</param>
		/// <param name="RequestedContract">The identity of the requestor</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="Purpose">The purpose of the petition.</param>
		public PetitionContractNavigationArgs(LegalIdentity? RequestorIdentity, string? RequestorFullJid, Contract? RequestedContract,
				string? PetitionId, string? Purpose)
		{
			this.RequestorIdentity = RequestorIdentity;
			this.RequestorFullJid = RequestorFullJid;
			this.RequestedContract = RequestedContract;
			this.PetitionId = PetitionId;
			this.Purpose = Purpose;
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
