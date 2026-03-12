using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.TravelDocuments.PACE.Id_PACE_ECDH_GM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-GM-AES-CBC-CMAC-256
	/// Generic Mapping, Elliptic Curve Diffie-Hellman, AES-CBC with CMAC, 256 bit key.
	/// </summary>
	public class Id_PACE_ECDH_GM_AES_CBC_CMAC_256() : PaceEcdhProtocol256()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.2.4";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Perfect;

		/// <summary>
		/// Authenticates the application with the document, using the PACE protocol.
		/// </summary>
		/// <param name="Client">Client connected to the document.</param>
		/// <returns>true if authenticated, false if unable to authenticate with the document.</returns>
		public override Task<bool> Authenticate(TravelDocumentsClient Client)
		{
			return Client.AuthenticateGenericMapping(this);
		}

		/// <summary>
		/// Gets the authenticator
		/// </summary>
		/// <param name="Key">Key to use for authenticator.</param>
		/// <returns>Authenticator</returns>
		public override CMac GetAuthenticator(byte[] Key) => CMac.CreateAes256CMac(Key);
	}
}
