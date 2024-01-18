using NeuroAccessMaui.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a petition of a signature.
	/// </summary>
	public class PetitionSignatureNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="PetitionSignatureNavigationArgs"/> class.
		/// </summary>
		public PetitionSignatureNavigationArgs()
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="PetitionSignatureNavigationArgs"/> class.
		/// </summary>
		/// <param name="requestorIdentity">The identity of the requestor.</param>
		/// <param name="requestorFullJid">The full Jid of the requestor.</param>
		/// <param name="signatoryIdentityId">Legal ID requested to sign <see cref="ContentToSign"/>.</param>
		/// <param name="contentToSign">Digital content to sign.</param>
		/// <param name="petitionId">The petition id.</param>
		/// <param name="purpose">The purpose of the petition.</param>
		public PetitionSignatureNavigationArgs(
			LegalIdentity requestorIdentity,
			string requestorFullJid,
			string signatoryIdentityId,
			byte[] contentToSign,
			string petitionId,
			string purpose)
		{
			this.RequestorIdentity = requestorIdentity;
			this.RequestorFullJid = requestorFullJid;
			this.SignatoryIdentityId = signatoryIdentityId;
			this.ContentToSign = contentToSign;
			this.PetitionId = petitionId;
			this.Purpose = purpose;
		}

		/// <summary>
		/// The identity of the requestor.
		/// </summary>
		public LegalIdentity? RequestorIdentity { get; }

		/// <summary>
		/// The full Jid of the requestor.
		/// </summary>
		public string? RequestorFullJid { get; }

		/// <summary>
		/// Legal ID requested to sign <see cref="ContentToSign"/>.
		/// </summary>
		public string? SignatoryIdentityId { get; }

		/// <summary>
		/// Digital content to sign.
		/// </summary>
		public byte[]? ContentToSign { get; }

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
