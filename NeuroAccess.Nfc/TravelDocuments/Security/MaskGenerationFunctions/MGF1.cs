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

			return true;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return "MGF1(" + this.hashFunction.ToString() + ")";
		}
	}
}
