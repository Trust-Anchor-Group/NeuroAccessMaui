using Waher.Security.SHA3;

namespace NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions
{
	/// <summary>
	/// SHA3-384
	/// </summary>
	public class Sha3_384 : HashFunction
	{
		/// <summary>
		/// SHA3-384
		/// </summary>
		public override string Oid => "2.16.840.1.101.3.4.2.9";

		/// <summary>
		/// Computes a Hash Digest from binary data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <returns>Hash Digest of binary data.</returns>
		public override byte[] ComputeHash(byte[] Data)
		{
			SHA3_384 H = new();
			return H.ComputeVariable(Data);
		}
	}
}
