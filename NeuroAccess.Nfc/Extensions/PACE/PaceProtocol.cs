using System;
using System.Numerics;
using System.Security.Cryptography;
using NeuroAccess.Nfc.Extensions.BAC;
using Waher.Content;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols.
	/// </summary>
	public abstract class PaceProtocol() : IPaceProtocol
	{
		private BigInteger version;
		private BigInteger? parameterId;

		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		public abstract string Oid { get; }

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		public abstract Grade SecurityStrength { get; }

		/// <summary>
		/// If Chip-Authentication-Mapping is supported by the protocol.
		/// </summary>
		public virtual bool ChipAuthenticationMapping => false;

		/// <summary>
		/// Required protocol version.
		/// </summary>
		public BigInteger Version => this.version;

		/// <summary>
		/// Optional Parameter ID.
		/// </summary>
		public BigInteger? ParameterId => this.parameterId;

		/// <summary>
		/// If the interface understands objects such as Object.
		/// </summary>
		/// <param name="Object">OID</param>
		/// <returns>How well objects of this type are supported.</returns>
		public Grade Supports(string Object)
		{
			return Object == this.Oid ? this.SecurityStrength : Grade.NotAtAll;
		}

		/// <summary>
		/// Bits of security provided by the protocol.
		/// </summary>
		public abstract int Bits { get; }

		/// <summary>
		/// If the protocol could be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the protocol could be configured, given the security information.</returns>
		public virtual bool Configure(Array SecurityInfo)
		{
			if (SecurityInfo.Length < 2)
				return false;

			if (SecurityInfo.GetValue(1) is not BigInteger Version)
				return false;

			this.version = Version;

			if (SecurityInfo.Length > 3)
				return false;
			else if (SecurityInfo.Length == 2)
				return true;
			else if (SecurityInfo.GetValue(2) is not BigInteger ParameterId)
				return false;
			else
			{
				this.parameterId = ParameterId;
				return true;
			}
		}

		/// <summary>
		/// Creates a new ephemeral key, used in the PACE protocol.
		/// </summary>
		/// <returns>Public part of the ephemeral key.</returns>
		public virtual byte[] CreateNewEphemeralKey()
		{
			// TODO
			throw new NotImplementedException("This protocol does not implement ephemeral key generation.");
		}

		/// <summary>
		/// Hash function to use when deriving keys from the shared secret.
		/// </summary>
		/// <param name="Data">Data to hash.</param>
		/// <returns>Hash digest.</returns>
		public abstract byte[] HashFunction(byte[] Data);

		/// <summary>
		/// Gets the shared secret, given the local private key previously generated using
		/// <see cref="CreateNewEphemeralKey"/> and a remote public key.
		/// </summary>
		/// <param name="RemotePublicKey">Remote public key.</param>
		/// <returns>Shared secret</returns>
		public virtual byte[] GetSharedSecret(byte[] RemotePublicKey)
		{
			// TODO
			throw new NotImplementedException("This protocol does not implement ephemeral key generation.");
		}

		/// <summary>
		/// Seed for computing cryptographic keys (§D.2)
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] K(DocumentInformation Info)
		{
			byte[] Data = InternetContent.ISO_8859_1.GetBytes(Info.MRZ_Information);
			return Hashes.ComputeSHA1Hash(Data);
		}

		/// <summary>
		/// KDF_Enc
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] KDF_Enc(DocumentInformation Info, bool AdjustParity)
		{
			return BacProtocol.KEnc(Info);  // Only uses the first 16 bytes of K.
			//return KDF(Info, 1, AdjustParity);  // KDF(K,1)
		}

		/// <summary>
		/// KDF_Mac
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] KDF_Mac(DocumentInformation Info, bool AdjustParity)
		{
			return BacProtocol.KMac(Info);  // Only uses the first 16 bytes of K.
			//return KDF(Info, 2, AdjustParity);  // KDF(K,2)
		}

		/// <summary>
		/// KDFπ, as defined in §9.7.3 of ICAO 9303-11.
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] KDFπ(DocumentInformation Info, bool AdjustParity)
		{
			return KDF(Info, 3, AdjustParity);  // KDF(K,3)
		}

		private static byte[] KDF(DocumentInformation Info, int Counter, bool AdjustParity)
		{
			byte[] K = PaceProtocol.K(Info);
			return BacProtocol.KDF(K, Counter, AdjustParity);
		}

		/// <summary>
		/// Calculates Kπ, given the shared secret and the document information.
		/// </summary>
		/// <param name="DocInfo">Document information.</param>
		/// <returns>Kπ value.</returns>
		public byte[] Kπ(DocumentInformation DocInfo)
		{
			return KDFπ(DocInfo, this.AdjustParity);
		}

		/// <summary>
		/// If parity of bytes in Kπ should be adjusted (3DES).
		/// </summary>
		public virtual bool AdjustParity => false;

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Info">Document information.</param>
		/// <param name="AdjustParity">If parity of bytes in Kπ should be adjusted (3DES).</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		public byte[] DecryptNonce(DocumentInformation Info, bool AdjustParity, byte[] EncryptedNonce)
		{
			byte[] Kπ = KDFπ(Info, AdjustParity);
			return this.DecryptNonce(Kπ, EncryptedNonce);
		}

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Kπ">Key derived from the document information.</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		public virtual byte[] DecryptNonce(byte[] Kπ, byte[] EncryptedNonce)
		{
			return DecryptNonceAes(Kπ, EncryptedNonce);
		}

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Kπ">Key derived from the document information.</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		public static byte[] DecryptNonceAes(byte[] Kπ, byte[] EncryptedNonce)
		{
			using Aes Cipher = Aes.Create();
			Cipher.Mode = CipherMode.CBC;
			Cipher.Padding = PaddingMode.None;
			Cipher.BlockSize = 128;
			Cipher.KeySize = 128;

			using ICryptoTransform Decryptor = Cipher.CreateDecryptor(Kπ, zeroIv16);

			return Decryptor.TransformFinalBlock(EncryptedNonce, 0, EncryptedNonce.Length);
		}

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Kπ">Key derived from the document information.</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		public static byte[] DecryptNonce3Des(byte[] Kπ, byte[] EncryptedNonce)
		{
			using TripleDES Cipher = TripleDES.Create();
			Cipher.Mode = CipherMode.CBC;
			Cipher.Padding = PaddingMode.None;

			using ICryptoTransform Decryptor = Cipher.CreateDecryptor(Kπ, zeroIv8);

			return Decryptor.TransformFinalBlock(EncryptedNonce, 0, EncryptedNonce.Length);
		}

		private static readonly byte[] zeroIv16 = new byte[16];
		private static readonly byte[] zeroIv8 = new byte[8];

	}
}
