using System.Security.Cryptography;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions
{
	/// <summary>
	/// SHA-1
	/// </summary>
	public class Sha1 : HashFunction
	{
		/// <summary>
		/// SHA-1
		/// </summary>
		public override string Oid => "1.3.14.3.2.26";

		/// <summary>
		/// Computes a Hash Digest from binary data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <returns>Hash Digest of binary data.</returns>
		public override byte[] ComputeHash(byte[] Data) => Hashes.ComputeSHA1Hash(Data);

		/// <summary>
		/// Hash Algorithm Name
		/// </summary>
		public override HashAlgorithmName Name => HashAlgorithmName.SHA1;
	}
}
