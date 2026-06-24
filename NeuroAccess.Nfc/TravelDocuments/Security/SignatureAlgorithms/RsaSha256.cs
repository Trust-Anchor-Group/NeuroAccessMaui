using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-256 with RSA Encryption
	/// </summary>
	public class RsaSha256 : RsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.11";


		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashFunctionArray HashAlgorithm => Hashes.ComputeSHA256Hash;

		/// <summary>
		/// OID of Hash algorithm to use.
		/// </summary>
		public override string HashAlgorithmOid => "2.16.840.1.101.3.4.2.1";
	}
}
