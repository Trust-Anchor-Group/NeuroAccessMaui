using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-1 with RSA Encryption
	/// </summary>
	public class RsaSha1 : RsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.5";


		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashFunctionArray HashAlgorithm => Hashes.ComputeSHA1Hash;

		/// <summary>
		/// OID of Hash algorithm to use.
		/// </summary>
		public override string HashAlgorithmOid => "1.3.14.3.2.26";
	}
}
