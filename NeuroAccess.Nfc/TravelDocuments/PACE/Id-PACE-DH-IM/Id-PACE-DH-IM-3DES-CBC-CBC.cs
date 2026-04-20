using System;
using System.Xml;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.TravelDocuments.PACE.Id_PACE_DH_IM
{
	/// <summary>
	/// PACE protocol id-PACE-DH-IM-3DES-CBC-CBC
	/// Integrated Mapping, Diffie-Hellman, 3DES in CBC mode for encryption, and 3DES in CBC mode for message authentication.
	/// </summary>
	public class Id_PACE_DH_IM_3DES_CBC_CBC() : PaceDhProtocol112()
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public override string Oid => "0.4.0.127.0.7.2.2.4.3.1";

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public override Grade SecurityStrength => Grade.NotAtAll;   // TODO: When implemented: Grade.Barely;

		/// <summary>
		/// If parity of bytes in Kπ should be adjusted (3DES).
		/// </summary>
		public override bool AdjustParity => true;

		/// <summary>
		/// Number of bytes used for sequence counter.
		/// </summary>
		public override int BlockLength => 8;

		/// <summary>
		/// Creates a new ephemeral key, used in the PACE protocol.
		/// </summary>
		/// <param name="Seed">Optional seed value for key generation.</param>
		/// <param name="Index">Index value for key generation.</param>
		/// <returns>Public part of the ephemeral key.</returns>
		public override byte[] CreateNewKey(byte[]? Seed, ref int Index)
		{
			throw new NotImplementedException("3DES not implemented.");  // TODO
		}

		/// <summary>
		/// Imports the private key and public key from a previous export.
		/// </summary>
		/// <returns>Public part of imported ephemeral key.</returns>
		public override byte[] ImportKey(XmlDocument Xml)
		{
			throw new NotImplementedException("3DES not implemented.");  // TODO
		}

		/// <summary>
		/// Encrypts data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data to be encrypted.</param>
		/// <returns>Encrypted data.</returns>
		public override byte[] Encrypt(byte[] Key, byte[] IV, byte[] Data)
		{
			return this.Encrypt3Des(Key, IV, Data);
		}

		/// <summary>
		/// Decrypts data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data to be decrypted.</param>
		/// <returns>Decrypted data.</returns>
		public override byte[] Decrypt(byte[] Key, byte[] IV, byte[] Data)
		{
			return this.Decrypt3Des(Key, IV, Data);
		}

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Kπ">Key derived from the document information.</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		public override byte[] DecryptNonce(byte[] Kπ, byte[] EncryptedNonce)
		{
			return this.Decrypt3Des(Kπ, zeroIv8, EncryptedNonce);
		}

		/// <summary>
		/// Gets the authenticator
		/// </summary>
		/// <param name="Key">Key to use for authenticator.</param>
		/// <returns>Authenticator</returns>
		public override CMac GetAuthenticator(byte[] Key) => throw new NotImplementedException("CMAC 3DES not implemented.");  // TODO
	}
}
