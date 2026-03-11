using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_ECDH_GM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-GM-AES-CBC-CMAC-128
	/// Generic Mapping, Elliptic Curve Diffie-Hellman, AES-CBC with CMAC, 128 bit key.
	/// </summary>
	public class Id_PACE_ECDH_GM_AES_CBC_CMAC_128() : PaceEcdhProtocol128()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.2.2";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Ok;

		/// <summary>
		/// Authenticates the application with the document, using the PACE protocol.
		/// </summary>
		/// <param name="IsoDep">NFC interface for communicating with the document.</param>
		/// <param name="DocInfo">Document information.</param>
		/// <param name="DocInfo">Document information.</param>
		/// <returns>true if authenticated, false if unable to authenticate with the document.</returns>
		public override Task<bool> Authenticate(IIsoDepInterface IsoDep, DocumentInformation DocInfo)
		{
			return GenericMapping.Authenticate(IsoDep, DocInfo, this);
		}

		/// <summary>
		/// Gets the authenticator
		/// </summary>
		/// <param name="Key">Key to use for authenticator.</param>
		/// <returns>Authenticator</returns>
		public override CMac GetAuthenticator(byte[] Key) => CMac.CreateAes128CMac(Key);
	}
}
