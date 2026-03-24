using System.Security.Cryptography;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-512 with ECDSA
	/// </summary>
	public class EcdsaSha512 : EcdsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.10045.4.3.4";

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashAlgorithmName HashAlgorithmName => HashAlgorithmName.SHA512;
	}
}
