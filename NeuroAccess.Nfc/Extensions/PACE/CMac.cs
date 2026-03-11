using System;
using System.Security.Cryptography;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Implements the CMAC algorithm, as defined in NIST SP 800-38B, revision 2016. Ref:
	/// https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-38b.pdf
	/// </summary>
	public class CMac : IDisposable
	{
		private readonly SymmetricAlgorithm cipher;
		private readonly byte[] key;
		private readonly byte[] subKey1;
		private readonly byte[] subKey2;
		private readonly int blockSizeBits;
		private readonly int blockSizeBytes;
		private bool disposed = false;

		/// <summary>
		/// Implements the CMAC algorithm, as defined in NIST SP 800-38B, revision 2016. Ref:
		/// https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-38b.pdf
		/// </summary>
		/// <param name="Cipher">Block Cipher</param>
		/// <param name="Key">CMAC Key</param>
		/// <param name="K1">Subkey 1</param>
		/// <param name="K2">Subkey 2</param>
		private CMac(SymmetricAlgorithm Cipher, byte[] Key, byte[] K1, byte[] K2)
		{
			this.cipher = Cipher;
			this.key = Key;
			this.subKey1 = K1;
			this.subKey2 = K2;
			this.blockSizeBits = this.cipher.BlockSize;
			this.blockSizeBytes = this.blockSizeBits >> 3;
		}

		/// <summary>
		/// Creates an instance of CMAC using AES-128 as the underlying block cipher
		/// </summary>
		/// <param name="Key">CMAC Key</param>
		/// <returns>CMAC object instance.</returns>
		public static CMac CreateAes128CMac(byte[] Key)
		{
			return CreateAesCMac(Key, zero16, 128, 128);
		}

		/// <summary>
		/// Creates an instance of CMAC using AES-192 as the underlying block cipher
		/// </summary>
		/// <param name="Key">CMAC Key</param>
		/// <returns>CMAC object instance.</returns>
		public static CMac CreateAes192CMac(byte[] Key)
		{
			return CreateAesCMac(Key, zero16, 128, 192);
		}

		/// <summary>
		/// Creates an instance of CMAC using AES-256 as the underlying block cipher
		/// </summary>
		/// <param name="Key">CMAC Key</param>
		/// <returns>CMAC object instance.</returns>
		public static CMac CreateAes256CMac(byte[] Key)
		{
			return CreateAesCMac(Key, zero16, 128, 256);
		}

		private static CMac CreateAesCMac(byte[] Key, byte[] Zero, int BlockSize, int KeySize)
		{
			if (Zero is null)
				throw new ArgumentNullException(nameof(Zero));

			int BlockByteSize = BlockSize >> 3;

			if (Zero.Length != BlockByteSize)
				throw new ArgumentException("Zero block must match block size.", nameof(Zero));

			Aes Aes = Aes.Create();
			Aes.Mode = CipherMode.CBC;
			Aes.Padding = PaddingMode.None;
			Aes.BlockSize = BlockSize;
			Aes.KeySize = KeySize;

			// Ref §6.1: Subkey Generation

			using ICryptoTransform Encryptor = Aes.CreateEncryptor(Key, Zero);

			byte[] L = Encryptor.TransformFinalBlock(Zero, 0, BlockByteSize);
			byte[] K1, K2;

			K1 = ShiftLeft(L);

			if ((L[0] & 0x80) != 0)
				K1[BlockByteSize - 1] ^= 0b10000111;    // R128

			K2 = ShiftLeft(K1);
			if ((K1[0] & 0x80) != 0)
				K2[BlockByteSize - 1] ^= 0b10000111;    // R128

			return new CMac(Aes, Key, K1, K2);
		}

		private static byte[] ShiftLeft(byte[] Data)
		{
			int c = Data.Length;
			byte[] Result = (byte[])Data.Clone();
			bool Carry = false;

			while (c-- > 0)
			{
				Result[c] <<= 1;
				if (Carry)
					Result[c] |= 1;

				Carry = (Data[c] & 0x80) != 0;
			}

			return Result;
		}

		/// <summary>
		/// Disposes of the object.
		/// </summary>
		public void Dispose()
		{
			if (!this.disposed)
			{
				this.disposed = true;

				Array.Clear(this.key, 0, this.key.Length);
				Array.Clear(this.subKey1, 0, this.subKey1.Length);
				Array.Clear(this.subKey2, 0, this.subKey2.Length);

				this.cipher.Dispose();
			}
		}

		/// <summary>
		/// Signs a message using the current CMAC.
		/// </summary>
		/// <param name="Message">Message to sign.</param>
		/// <param name="Len">Number of bytes of signature requested.</param>
		/// <returns>Most significant bits of CMAC signature</returns>
		public byte[] Sign(byte[] Message, int Len)
		{
			if (Len > this.blockSizeBytes)
				throw new ArgumentException("Requested signature length exceeds block size.", nameof(Len));

			byte[] C = this.Sign(Message);
			if (Len == this.blockSizeBytes)
				return C;

			byte[] C2 = new byte[Len];
			Buffer.BlockCopy(C, 0, C2, 0, Len);

			return C2;
		}

		/// <summary>
		/// Signs a message using the current CMAC.
		/// </summary>
		/// <param name="Message">Message to sign.</param>
		/// <returns>CMAC signature</returns>
		public byte[] Sign(byte[] Message)
		{
			// See §6.2: MAC Generation

			if (Message is null)
				throw new ArgumentNullException(nameof(Message));

			using ICryptoTransform Cipher = this.cipher.CreateEncryptor(this.key, zero16);

			byte[] M = new byte[this.blockSizeBytes];
			byte[] C = new byte[this.blockSizeBytes];
			int i = 0;
			int c = Message.Length;
			int d;
			int n = (c + this.blockSizeBytes - 1) / this.blockSizeBytes;
			if (n == 0)
				n = 1;

			do
			{
				d = c - i;
				if (d >= this.blockSizeBytes)
				{
					Buffer.BlockCopy(Message, i, M, 0, this.blockSizeBytes);
					i += this.blockSizeBytes;

					if (i >= c)
						XorInto(M, this.subKey1);
				}
				else
				{
					Buffer.BlockCopy(Message, i, M, 0, d);
					i += d;

					M[d++] = 0x80;
					if (d < this.blockSizeBytes)
						Array.Clear(M, d, this.blockSizeBytes - d);

					XorInto(M, this.subKey2);
				}

				XorInto(M, C);

				byte[] C2 = Cipher.TransformFinalBlock(M, 0, this.blockSizeBytes);

				Array.Clear(C, 0, this.blockSizeBytes);
				C = C2;
			}
			while (i < c);

			Array.Clear(M, 0, this.blockSizeBytes);

			return C;
		}

		private static readonly byte[] zero16 = new byte[16];

		private static void XorInto(byte[] Data, byte[] Data2)
		{
			int c = Data.Length;
			if (Data2.Length != c)
				throw new ArgumentException("Data blocks must be of the same length.", nameof(Data2));

			for (int i = 0; i < c; i++)
				Data[i] ^= Data2[i];
		}

		/// <summary>
		/// Verifies a CMAC Signature.
		/// </summary>
		/// <param name="Message">Message over which the signature was made.</param>
		/// <param name="Signature">CMAC signature.</param>
		/// <returns>If the signature is valid.</returns>
		public bool Verify(byte[] Message, byte[] Signature)
		{
			int i, c;

			if (Message is null || Signature is null || (c = Signature.Length) > this.blockSizeBytes)
				return false;

			byte[] Signature2 = this.Sign(Message, c);

			for (i = 0; i < c; i++)
			{
				if (Signature[i] != Signature2[i])
					return false;
			}

			return true;
		}
	}
}
