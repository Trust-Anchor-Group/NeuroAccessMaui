using System.Security.Cryptography;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-512 with RSA Encryption
	/// </summary>
	public class RsaSha512 : RsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.13";

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashAlgorithmName HashAlgorithmName => HashAlgorithmName.SHA512;
	}
}
