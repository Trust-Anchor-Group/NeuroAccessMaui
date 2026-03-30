using System.Security.Cryptography;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions
{
	/// <summary>
	/// SHA-384
	/// </summary>
	public class Sha384 : HashFunction
	{
		/// <summary>
		/// SHA-384
		/// </summary>
		public override string Oid => "2.16.840.1.101.3.4.2.2";

		/// <summary>
		/// Computes a Hash Digest from binary data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <returns>Hash Digest of binary data.</returns>
		public override byte[] ComputeHash(byte[] Data) => Hashes.ComputeSHA384Hash(Data);

		/// <summary>
		/// Hash Algorithm Name
		/// </summary>
		public override HashAlgorithmName Name => HashAlgorithmName.SHA384;
	}
}
