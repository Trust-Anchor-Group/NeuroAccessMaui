using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE.Id_PACE_ECDH_GM
{
	/// <summary>
	/// PACE protocol id-PACE-ECDH-GM-3DES-CBC-CBC
	/// Generic Mapping, Elliptic Curve Diffie-Hellman, 3DES in CBC mode for encryption, and 3DES in CBC mode for message authentication.
	/// </summary>
	public class Id_PACE_ECDH_GM_3DES_CBC_CBC() : PaceEcdhProtocol112()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.2.1";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.Barely;

		/// <summary>
		/// If parity of bytes in Kπ should be adjusted (3DES).
		/// </summary>
		public override bool AdjustParity => true;

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Kπ">Key derived from the document information.</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		public override byte[] DecryptNonce(byte[] Kπ, byte[] EncryptedNonce)
		{
			return DecryptNonce3Des(Kπ, EncryptedNonce);
		}
	}
}
