using System;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.TravelDocuments.PACE.Id_PACE_DH_GM
{
	/// <summary>
	/// PACE protocol id-PACE-DH-GM-3DES-CBC-CBC
	/// Generic Mapping, Diffie-Hellman, 3DES in CBC mode for encryption, and 3DES in CBC mode for message authentication.
	/// </summary>
	public class Id_PACE_DH_GM_3DES_CBC_CBC() : PaceDhProtocol112()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.1.1";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.NotAtAll;   // TODO: When implemented: Grade.Barely;

		/// <summary>
		/// If parity of bytes in Kπ should be adjusted (3DES).
		/// </summary>
		public override bool AdjustParity => true;

		/// <summary>
		/// Creates a new ephemeral key, used in the PACE protocol.
		/// </summary>
		/// <returns>Public part of the ephemeral key.</returns>
		public override byte[] CreateNewKey()
		{
			throw new NotImplementedException("3DES not implemented.");  // TODO
		}

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

		/// <summary>
		/// Gets the authenticator
		/// </summary>
		/// <param name="Key">Key to use for authenticator.</param>
		/// <returns>Authenticator</returns>
		public override CMac GetAuthenticator(byte[] Key) => throw new NotImplementedException("CMAC 3DES not implemented.");  // TODO
	}
}
