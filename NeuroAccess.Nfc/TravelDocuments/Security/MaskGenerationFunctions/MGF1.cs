using System;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;

namespace NeuroAccess.Nfc.TravelDocuments.Security.MaskGenerationFunctions
{
	/// <summary>
	/// Mask Generation Function MGF1, as defined in RFC 4055.
	/// </summary>
	/// <param name="HashFunction">Hash function to use.</param>
	public class MGF1(HashFunction HashFunction) : MaskGenerationFunction
	{
		private static readonly HashFunction defaultHashFunction = new Sha1();

		private HashFunction hashFunction = HashFunction;
		private bool configured;

		/// <summary>
		/// Mask Generation Function MGF1, as defined in RFC 4055, using SHA-1.
		/// </summary>
		public MGF1()
			: this(defaultHashFunction)
		{
		}

		/// <summary>
		/// SHA-1
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.8";

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.configured;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length >= 2)
			{
				if (SecurityInfo[1] is not HashFunction HashFunction)
					return false;

				this.hashFunction = HashFunction;
			}

			this.configured = true;

			return true;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return "MGF1(" + this.hashFunction.ToString() + ")";
		}

		/// <summary>
		/// Calcaultes a mask of a specific length, given a seed.
		/// </summary>
		/// <param name="Seed">Seed value.</param>
		/// <param name="Length">Length of mask.</param>
		/// <returns>Generated mask.</returns>
		public override byte[] CalculateMask(byte[] Seed, int Length)
		{
			if (Length < 0)
				throw new ArgumentOutOfRangeException(nameof(Length), "Length must be non-negative.");

			byte[] Result = new byte[Length];
			byte[] C = new byte[Seed.Length + 4];
			int i = 0;

			Buffer.BlockCopy(Seed, 0, C, 0, Seed.Length);

			while (true)
			{
				byte[] T = this.hashFunction.ComputeHash(C);
				int d = Math.Min(T.Length, Length - i);

				Buffer.BlockCopy(T, 0, Result, i, d);
				i += d;

				if (i == Length)
					return Result;

				d = C.Length;
				while (d > 0)
				{
					if (++C[--d] != 0)
						break;
				}
			}
		}
	}
}
