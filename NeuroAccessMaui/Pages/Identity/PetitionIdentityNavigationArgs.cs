using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Identity.PetitionIdentity
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a petition of a legal identity.
    /// </summary>
    public class PetitionIdentityNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="PetitionIdentityNavigationArgs"/> class.
        /// </summary>
        public PetitionIdentityNavigationArgs() { }

        /// <summary>
        /// Creates an instance of the <see cref="PetitionIdentityNavigationArgs"/> class.
        /// </summary>
        /// <param name="requestorIdentity">The identity of the requestor.</param>
        /// <param name="requestorFullJid">The full Jid of the requestor.</param>
        /// <param name="requestedIdentityId">The requested identity id.</param>
        /// <param name="petitionId">The petition id.</param>
        /// <param name="purpose">The purpose of the petition.</param>
        public PetitionIdentityNavigationArgs(
                LegalIdentity requestorIdentity,
                string requestorFullJid,
                string requestedIdentityId,
                string petitionId,
                string purpose)
        {
            this.RequestorIdentity = requestorIdentity;
            this.RequestorFullJid = requestorFullJid;
            this.RequestedIdentityId = requestedIdentityId;
            this.PetitionId = petitionId;
            this.Purpose = purpose;
        }
        /// <summary>
        /// The identity of the requestor.
        /// </summary>
        public LegalIdentity RequestorIdentity { get; }
        /// <summary>
        /// The full Jid of the requestor.
        /// </summary>
        public string RequestorFullJid { get; }
        /// <summary>
        /// The requested identity id.
        /// </summary>
        public string RequestedIdentityId { get; }
        /// <summary>
        /// The petition id.
        /// </summary>
        public string PetitionId { get; }
        /// <summary>
        /// The purpose of the petition.
        /// </summary>
        public string Purpose { get; }
    }
}