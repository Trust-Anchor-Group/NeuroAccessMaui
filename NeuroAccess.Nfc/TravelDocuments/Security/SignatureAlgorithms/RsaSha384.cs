using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-384 with RSA Encryption
	/// </summary>
	public class RsaSha384 : RsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.12";


		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashFunctionArray HashAlgorithm => Hashes.ComputeSHA384Hash;

		/// <summary>
		/// OID of Hash algorithm to use.
		/// </summary>
		public override string HashAlgorithmOid => "2.16.840.1.101.3.4.2.2";
	}
}
