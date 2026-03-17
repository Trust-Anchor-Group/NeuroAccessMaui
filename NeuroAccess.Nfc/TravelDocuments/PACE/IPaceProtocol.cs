using System;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.PACE
{
	/// <summary>
	/// Basic interface for PACE protocols.
	/// </summary>
	public interface IPaceProtocol : ISecurityObject
	{
		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		Grade SecurityStrength { get; }

		/// <summary>
		/// If Chip-Authentication-Mapping is supported by the protocol.
		/// </summary>
		bool ChipAuthenticationMapping { get; }

		/// <summary>
		/// Creates a new private and public key, used in the PACE protocol.
		/// </summary>
		/// <returns>Public part of the ephemeral key.</returns>
		byte[] CreateNewKey();

		/// <summary>
		/// Creates an ephemeral key using the same algorithm, cipher and configuration.
		/// </summary>
		/// <returns>Ephemeral key</returns>
		IPaceProtocol CreateEphemeralKey();

		/// <summary>
		/// Hash function to use in the KDF function.
		/// </summary>
		HashFunctionArray KdfHashFunction { get; }

		/// <summary>
		/// Number of bytes of hash output to use as key material in the KDF function.
		/// </summary>
		int KdfHashKeyLength { get; }

		/// <summary>
		/// Number of bytes used for blocks.
		/// </summary>
		int BlockLength { get; }

		/// <summary>
		/// Gets the shared secret, given the local private key previously generated using
		/// <see cref="CreateNewKey"/> and a remote public key.
		/// </summary>
		/// <param name="RemotePublicKey">Remote public key.</param>
		/// <returns></returns>
		byte[] GetSharedSecret(byte[] RemotePublicKey);

		/// <summary>
		/// Calculates Kπ, given the shared secret and the document information.
		/// </summary>
		/// <param name="DocInfo">Document information.</param>
		/// <returns>Kπ value.</returns>
		byte[] KDFπ(DocumentInformation DocInfo);

		/// <summary>
		/// KDF_Enc
		/// </summary>
		/// <param name="KSeed">Key derivation seed value.</param>
		byte[] KDF_Enc(byte[] KSeed);

		/// <summary>
		/// KDF_Mac
		/// </summary>
		/// <param name="KSeed">Key derivation seed value.</param>
		byte[] KDF_Mac(byte[] KSeed);

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Kπ">Key derived from the document information.</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		byte[] DecryptNonce(byte[] Kπ, byte[] EncryptedNonce);

		/// <summary>
		/// Authenticates the application with the document, using the PACE protocol.
		/// </summary>
		/// <param name="Client">Client connected to the document.</param>
		/// <returns>true if authenticated, false if unable to authenticate with the document.</returns>
		Task<bool> Authenticate(TravelDocumentsClient Client);

		/// <summary>
		/// Gets the authenticator
		/// </summary>
		/// <param name="Key">Key to use for authenticator.</param>
		/// <returns>Authenticator</returns>
		CMac GetAuthenticator(byte[] Key);

		/// <summary>
		/// Encrypts data.
		/// </summary>
		/// <param name="KS_Enc">Session encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data to be encrypted.</param>
		/// <returns>Encrypted data.</returns>
		byte[] Encrypt(byte[] KS_Enc, byte[] IV, byte[] Data);

		/// <summary>
		/// Decrypts data.
		/// </summary>
		/// <param name="KS_Enc">Session encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data to be decrypted.</param>
		/// <returns>Decrypted data.</returns>
		byte[] Decrypt(byte[] KS_Enc, byte[] IV, byte[] Data);
	}
}
