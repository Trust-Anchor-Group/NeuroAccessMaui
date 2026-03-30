using System.Security.Cryptography;

namespace NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions
{
	/// <summary>
	/// SHA3-512
	/// </summary>
	public class Sha3_512 : HashFunction
	{
		/// <summary>
		/// SHA3-512
		/// </summary>
		public override string Oid => "2.16.840.1.101.3.4.2.10";

		/// <summary>
		/// Computes a Hash Digest from binary data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <returns>Hash Digest of binary data.</returns>
		public override byte[] ComputeHash(byte[] Data)
		{
			Waher.Security.SHA3.SHA3_512 H = new();
			return H.ComputeVariable(Data);
		}

		/// <summary>
		/// Hash Algorithm Name
		/// </summary>
		public override HashAlgorithmName Name => HashAlgorithmName.SHA3_512;
	}
}
