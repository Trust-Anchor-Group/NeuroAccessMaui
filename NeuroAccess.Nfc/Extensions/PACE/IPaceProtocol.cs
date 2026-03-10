using System;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Basic interface for PACE protocols.
	/// </summary>
	public interface IPaceProtocol : IProcessingSupport<string>
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		string Oid { get; }

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		Grade SecurityStrength { get; }

		/// <summary>
		/// If Chip-Authentication-Mapping is supported by the protocol.
		/// </summary>
		bool ChipAuthenticationMapping { get; }

		/// <summary>
		/// If the protocol could be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the protocol could be configured, given the security information.</returns>
		bool Configure(Array SecurityInfo);

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
		/// Hash function to use when deriving keys from the shared secret.
		/// </summary>
		/// <param name="Data">Data to hash.</param>
		/// <returns>Hash digest.</returns>
		byte[] HashFunction(byte[] Data);

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
		byte[] Kπ(DocumentInformation DocInfo);

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Kπ">Key derived from the document information.</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		byte[] DecryptNonce(byte[] Kπ, byte[] EncryptedNonce);
	}
}
