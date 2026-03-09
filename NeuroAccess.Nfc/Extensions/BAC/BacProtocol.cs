using System;
using System.Security.Cryptography;
using Waher.Content;
using Waher.Security;

namespace NeuroAccess.Nfc.Extensions.BAC
{
	/// <summary>
	/// Implements Basic Access Control (BAC) key derivation.
	/// </summary>
	public static class BacProtocol
	{
		/// <summary>
		/// Seed for computing cryptographic keys (§D.2)
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] KSeed(DocumentInformation Info)
		{
			byte[] Data = InternetContent.ISO_8859_1.GetBytes(Info.MRZ_Information);
			byte[] H = Hashes.ComputeSHA1Hash(Data);
			Array.Resize(ref H, 16);
			return H;
		}

		/// <summary>
		/// BAC Encryption Key (3DES). Ref: §D.1
		/// </summary>
		public static byte[] KEnc(DocumentInformation Info)
		{
			return KDF(Info, 1, true);	// KDF(K,1)
		}

		/// <summary>
		/// BAC MAC Key (3DES). Ref: §D.1
		/// </summary>
		public static byte[] KMac(DocumentInformation Info)
		{
			return KDF(Info, 2, true);   // KDF(K,2)
		}

		private static byte[] KDF(DocumentInformation Info, int Counter, bool AdjustParity)
		{
			byte[] KSeed = BacProtocol.KSeed(Info);
			return KDF(KSeed, Counter, AdjustParity);
		}

		internal static byte[] KDF(byte[] KSeed, int Counter, bool AdjustParity)
		{
			int c = KSeed.Length;
			byte[] D = new byte[c + 4];
			Buffer.BlockCopy(KSeed, 0, D, 0, c);
			int i;

			for (i = c + 3; i >= c; i--)
			{
				D[i] = (byte)Counter;
				Counter >>= 8;
			}

			byte[] H = Hashes.ComputeSHA1Hash(D);
			Array.Resize(ref H, 16);

			if (AdjustParity)
				OddParity(H);

			return H;
		}

		private static void OddParity(byte[] H)
		{
			int i, j, c = H.Length;
			byte b;

			for (i = 0; i < c; i++)
			{
				b = H[i];
				j = 0;

				while (b != 0)
				{
					j += b & 1;
					b >>= 1;
				}

				if ((j & 1) == 0)
					H[i] ^= 1;
			}
		}

		/// <summary>
		/// Calculates a response to a challenge using 3DES & SHA1.
		/// </summary>
		/// <param name="Challenge">Challenge</param>
		/// <param name="Rnd1">Random number 1</param>
		/// <param name="Rnd2">Random number 2</param>
		/// <param name="KEnc">Encryption Key</param>
		/// <param name="KMac">MAC Key</param>
		/// <returns>Response</returns>
		public static byte[] CalcChallengeResponse3DES(byte[] Challenge, byte[] Rnd1, byte[] Rnd2,
			byte[] KEnc, byte[] KMac)
		{
			byte[] S = Rnd1.CONCAT(Challenge, Rnd2);
			byte[] EIFD;
			byte[] MIFD;

			using (TripleDES Cipher = TripleDES.Create())
			{
				Cipher.Mode = CipherMode.CBC;
				Cipher.Padding = PaddingMode.None;

				using ICryptoTransform Encryptor = Cipher.CreateEncryptor(KEnc, new byte[8]);
				EIFD = Encryptor.TransformFinalBlock(S, 0, 32);
			}

			// MAC Algorithm described in ISO/IEC 9797-1
			// Ref: https://en.wikipedia.org/wiki/ISO/IEC_9797-1

			using (DES Cipher = DES.Create())
			{
				Cipher.Mode = CipherMode.CBC;
				Cipher.Padding = PaddingMode.None;

				int i = 0;
				int c = EIFD.Length;
				int j;

				byte[] Data = new byte[c + 8];
				Buffer.BlockCopy(EIFD, 0, Data, 0, c);
				Data[c] = 0x80;   // Padding method 2, append 80 00 00 00 00 00 00 00

				byte[] Ka = new byte[8];
				byte[] Kb = new byte[8];

				Buffer.BlockCopy(KMac, 0, Ka, 0, 8);
				Buffer.BlockCopy(KMac, 8, Kb, 0, 8);

				byte[] Block = new byte[8];
				byte[]? H = null;

				c += 8;
				using (ICryptoTransform Encryptor2 = Cipher.CreateEncryptor(Ka, new byte[8]))
				{
					while (i < c)
					{
						Buffer.BlockCopy(Data, i, Block, 0, 8);
						i += 8;

						if (H is not null)
						{
							for (j = 0; j < 8; j++)
								Block[j] ^= H[j];
						}

						H = Encryptor2.TransformFinalBlock(Block, 0, 8);
					}

					using (ICryptoTransform FinalDecryptor = Cipher.CreateDecryptor(Kb, new byte[8]))
					{
						H = FinalDecryptor.TransformFinalBlock(H, 0, 8);
					}

					H = Encryptor2.TransformFinalBlock(H, 0, 8);
				}

				MIFD = H;
			}

			return EIFD.CONCAT(MIFD);
		}

		/// <summary>
		/// Calculates a response to a challenge using 3DES & SHA1.
		/// </summary>
		/// <param name="Info">Document Information</param>
		/// <param name="Challenge">Challenge</param>
		/// <returns>Response</returns>
		public static byte[] CalcChallengeResponse3DES(DocumentInformation Info, byte[] Challenge)
		{
			byte[] Rnd1 = new byte[8];
			byte[] Rnd2 = new byte[16];

			using (RandomNumberGenerator Rnd = RandomNumberGenerator.Create())
			{
				Rnd.GetBytes(Rnd1);
				Rnd.GetBytes(Rnd2);
			}

			return CalcChallengeResponse3DES(Challenge, Rnd1, Rnd2, KEnc(Info), KMac(Info));
		}

	}
}
