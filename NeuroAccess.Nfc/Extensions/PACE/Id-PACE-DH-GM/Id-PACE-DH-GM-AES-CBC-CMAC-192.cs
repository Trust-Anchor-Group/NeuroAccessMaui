using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_DH_GM
{
	/// <summary>
	/// PACE protocol id-PACE-DH-GM-AES-CBC-CMAC-192
	/// Generic Mapping, Diffie-Hellman, AES-CBC with CMAC, 192 bit key.
	/// </summary>
	public class Id_PACE_DH_GM_AES_CBC_CMAC_192() : PaceDhProtocol192()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.1.3";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.NotAtAll;   // TODO: When implemented: Grade.Ok;     // RSA cannot have a higher than Ok grade.

		/// <summary>
		/// Gets the authenticator
		/// </summary>
		/// <param name="Key">Key to use for authenticator.</param>
		/// <returns>Authenticator</returns>
		public override CMac GetAuthenticator(byte[] Key) => CMac.CreateAes192CMac(Key);
	}
}
