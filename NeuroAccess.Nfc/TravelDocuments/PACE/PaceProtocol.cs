using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.PACE
{
	/// <summary>
	/// Abstract base class for PACE protocols.
	/// </summary>
	public abstract class PaceProtocol() : SecurityObject, IPaceProtocol
	{
		private BigInteger version;
		private BigInteger? parameterId;

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
		/// Bits of security provided by the protocol.
		/// </summary>
		public abstract int Bits { get; }

		/// <summary>
		/// If the interface understands objects such as Object.
		/// </summary>
		/// <param name="Object">OID</param>
		/// <returns>How well objects of this type are supported.</returns>
		public override Grade Supports(string Object)
		{
			return Object == this.Oid ? this.SecurityStrength : Grade.NotAtAll;
		}

		/// <summary>
		/// If the protocol could be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the protocol could be configured, given the security information.</returns>
		public override bool Configure(Array SecurityInfo)
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
		public abstract byte[] CreateNewKey();

		/// <summary>
		/// Creates an ephemeral key using the same algorithm, cipher and configuration.
		/// </summary>
		/// <returns>Ephemeral key</returns>
		public abstract IPaceProtocol CreateEphemeralKey();

		/// <summary>
		/// Gets the shared secret, given the local private key previously generated using
		/// <see cref="CreateNewKey"/> and a remote public key.
		/// </summary>
		/// <param name="RemotePublicKey">Remote public key.</param>
		/// <returns>Shared secret</returns>
		public virtual byte[] GetSharedSecret(byte[] RemotePublicKey)
		{
			// TODO
			throw new NotImplementedException("This protocol does not implement ephemeral key generation.");
		}

		/// <summary>
		/// Authenticates the application with the document, using the PACE protocol.
		/// </summary>
		/// <param name="Client">Client connected to the document.</param>
		/// <returns>true if authenticated, false if unable to authenticate with the document.</returns>
		public virtual Task<bool> Authenticate(TravelDocumentsClient Client)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// KDFπ, as defined in §9.7.3 of ICAO 9303-11.
		/// </summary>
		/// <param name="Info">Document Information</param>
		public byte[] KDFπ(DocumentInformation Info)
		{
			return TravelDocumentsClient.KDF(TravelDocumentsClient.PACE_K(Info), 3,
				this.AdjustParity, this.KdfHashFunction, this.KdfHashKeyLength);
		}

		/// <summary>
		/// KDF_Enc
		/// </summary>
		/// <param name="KSeed">Key derivation seed value.</param>
		public byte[] KDF_Enc(byte[] KSeed)
		{
			return TravelDocumentsClient.KDF(KSeed, 1, this.AdjustParity, this.KdfHashFunction, this.KdfHashKeyLength);
		}

		/// <summary>
		/// KDF_Mac
		/// </summary>
		/// <param name="KSeed">Key derivation seed value.</param>
		public byte[] KDF_Mac(byte[] KSeed)
		{
			return TravelDocumentsClient.KDF(KSeed, 2, this.AdjustParity, this.KdfHashFunction, this.KdfHashKeyLength);
		}

		/// <summary>
		/// If parity of bytes in Kπ should be adjusted (3DES).
		/// </summary>
		public virtual bool AdjustParity => false;

		/// <summary>
		/// Hash function to use in the KDF function.
		/// </summary>
		public virtual HashFunctionArray KdfHashFunction => Hashes.ComputeSHA1Hash;

		/// <summary>
		/// Number of bytes of hash output to use as key material in the KDF function.
		/// </summary>
		public virtual int KdfHashKeyLength => 16;

		/// <summary>
		/// Number of bytes used for sequence counter.
		/// </summary>
		public virtual int BlockLength => 16;

		/// <summary>
		/// Decrypts an encrypted nonce value.
		/// </summary>
		/// <param name="Info">Document information.</param>
		/// <param name="AdjustParity">If parity of bytes in Kπ should be adjusted (3DES).</param>
		/// <param name="EncryptedNonce">Encrypted nonce.</param>
		/// <returns>Decrypted nonce.</returns>
		public byte[] DecryptNonce(DocumentInformation Info, byte[] EncryptedNonce)
		{
			byte[] Kπ = this.KDFπ(Info);
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
			return this.DecryptAes(Kπ, zeroIv16, EncryptedNonce);
		}

		/// <summary>
		/// Creates a block of associated data used for MAC signatures.
		/// </summary>
		/// <param name="Oid">OID of cipher.</param>
		/// <param name="PublicKey">Public key.</param>
		/// <returns>Associated Data block.</returns>
		public static byte[] CreateAssociatedData(string Oid, byte[] PublicKey)
		{
			string[] OidParts = Oid.Split('.');
			int i, c = OidParts.Length;
			byte[] OidBytes = new byte[c];

			for (i = 0; i < c; i++)
				OidBytes[i] = byte.Parse(OidParts[i], CultureInfo.InvariantCulture);

			int d = PublicKey.Length;
			byte[] AssociatedData = new byte[7 + c + d];

			AssociatedData[0] = 0x7f;
			AssociatedData[1] = 0x49;
			AssociatedData[2] = (byte)(4 + c + d);
			AssociatedData[3] = 0x06;
			AssociatedData[4] = (byte)(c - 1);

			Buffer.BlockCopy(OidBytes, 1, AssociatedData, 5, c - 1);
			i = 4 + c;

			AssociatedData[i++] = 0x86;
			AssociatedData[i++] = (byte)(1 + d);
			AssociatedData[i++] = 0x04;

			Buffer.BlockCopy(PublicKey, 0, AssociatedData, i, d);

			return AssociatedData;
		}

		/// <summary>
		/// Gets the authenticator
		/// </summary>
		/// <param name="Key">Key to use for authenticator.</param>
		/// <returns>Authenticator</returns>
		public abstract CMac GetAuthenticator(byte[] Key);

		/// <summary>
		/// Encrypts data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data to be encrypted.</param>
		/// <returns>Encrypted data.</returns>
		public virtual byte[] Encrypt(byte[] Key, byte[] IV, byte[] Data)
		{
			return this.EncryptAes(Key, IV, Data);
		}

		/// <summary>
		/// Decrypts data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data to be decrypted.</param>
		/// <returns>Decrypted data.</returns>
		public virtual byte[] Decrypt(byte[] Key, byte[] IV, byte[] Data)
		{
			return this.DecryptAes(Key, IV, Data);
		}
		/// <summary>
		/// Encrypts data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data.</param>
		/// <returns>Encrypted data.</returns>
		protected byte[] EncryptAes(byte[] Key, byte[] IV, byte[] Data)
		{
			using Aes Cipher = Aes.Create();
			Cipher.Mode = CipherMode.CBC;
			Cipher.Padding = PaddingMode.None;
			Cipher.BlockSize = this.BlockLength << 3;
			Cipher.KeySize = Key.Length << 3;

			using ICryptoTransform Encryptor = Cipher.CreateEncryptor(Key, IV);

			return Encryptor.TransformFinalBlock(Data, 0, Data.Length);
		}

		/// <summary>
		/// Encrypts data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="Data">Data.</param>
		/// <returns>Encrypted data.</returns>
		protected byte[] Encrypt3Des(byte[] Key, byte[] IV, byte[] Data)
		{
			using TripleDES Cipher = TripleDES.Create();
			Cipher.Mode = CipherMode.CBC;
			Cipher.Padding = PaddingMode.None;

			using ICryptoTransform Encryptor = Cipher.CreateEncryptor(Key, IV);

			return Encryptor.TransformFinalBlock(Data, 0, Data.Length);
		}

		/// <summary>
		/// Decrypts encrypted data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="EncryptedData">Encrypted data.</param>
		/// <returns>Decrypted data.</returns>
		protected byte[] DecryptAes(byte[] Key, byte[] IV, byte[] EncryptedData)
		{
			using Aes Cipher = Aes.Create();
			Cipher.Mode = CipherMode.CBC;
			Cipher.Padding = PaddingMode.None;
			Cipher.BlockSize = this.BlockLength << 3;
			Cipher.KeySize = Key.Length << 3;

			using ICryptoTransform Decryptor = Cipher.CreateDecryptor(Key, IV);

			return Decryptor.TransformFinalBlock(EncryptedData, 0, EncryptedData.Length);
		}

		/// <summary>
		/// Decrypts encrypted data.
		/// </summary>
		/// <param name="Key">Encryption key.</param>
		/// <param name="IV">Initialization vector.</param>
		/// <param name="EncryptedData">Encrypted data.</param>
		/// <returns>Decrypted data.</returns>
		protected byte[] Decrypt3Des(byte[] Key, byte[] IV, byte[] EncryptedData)
		{
			using TripleDES Cipher = TripleDES.Create();
			Cipher.Mode = CipherMode.CBC;
			Cipher.Padding = PaddingMode.None;

			using ICryptoTransform Decryptor = Cipher.CreateDecryptor(Key, IV);
			
			return Decryptor.TransformFinalBlock(EncryptedData, 0, EncryptedData.Length);
		}

		/// <summary>
		/// Initialization Vector of 16 zero bytes.
		/// </summary>
		protected static readonly byte[] zeroIv16 = new byte[16];

		/// <summary>
		/// Initialization Vector of 8 zero bytes.
		/// </summary>
		protected static readonly byte[] zeroIv8 = new byte[8];
	}
}
