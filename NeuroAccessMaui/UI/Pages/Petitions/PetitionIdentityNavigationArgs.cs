using NeuroAccessMaui.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Petitions
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a petition of a legal identity.
	/// </summary>
	/// <param name="RequestorIdentity">The identity of the requestor.</param>
	/// <param name="RequestorFullJid">The full Jid of the requestor.</param>
	/// <param name="RequestedIdentityId">The requested identity id.</param>
	/// <param name="PetitionId">The petition id.</param>
	/// <param name="Purpose">The purpose of the petition.</param>
	public class PetitionIdentityNavigationArgs(LegalIdentity? RequestorIdentity,
				string? RequestorFullJid = null, string? RequestedIdentityId = null,
				string? PetitionId = null, string? Purpose = null) : NavigationArgs
	{
		/// <summary>
		/// Creates a default instance.
		/// </summary>
		public PetitionIdentityNavigationArgs() : this(null) { }

		/// <summary>
		/// The identity of the requestor.
		/// </summary>
		public LegalIdentity? RequestorIdentity { get; } = RequestorIdentity;

		/// <summary>
		/// The full Jid of the requestor.
		/// </summary>
		public string? RequestorFullJid { get; } = RequestorFullJid;

		/// <summary>
		/// The requested identity id.
		/// </summary>
		public string? RequestedIdentityId { get; } = RequestedIdentityId;

		/// <summary>
		/// The petition id.
		/// </summary>
		public string? PetitionId { get; } = PetitionId;

		/// <summary>
		/// The purpose of the petition.
		/// </summary>
		public string? Purpose { get; } = Purpose;
	}
}
