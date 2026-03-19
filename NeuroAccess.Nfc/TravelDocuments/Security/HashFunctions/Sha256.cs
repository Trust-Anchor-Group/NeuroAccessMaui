using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions
{
	/// <summary>
	/// SHA-256
	/// </summary>
	public class Sha256 : HashFunction
	{
		/// <summary>
		/// SHA-256
		/// </summary>
		public override string Oid => "2.16.840.1.101.3.4.2.1";

		/// <summary>
		/// Computes a Hash Digest from binary data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <returns>Hash Digest of binary data.</returns>
		public override byte[] ComputeHash(byte[] Data) => Hashes.ComputeSHA256Hash(Data);
	}
}
