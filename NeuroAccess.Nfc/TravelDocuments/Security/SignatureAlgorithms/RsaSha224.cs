using System;
using System.Security.Cryptography;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-224 with RSA Encryption
	/// </summary>
	public class RsaSha224 : RsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.14";

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashAlgorithmName HashAlgorithmName =>
			throw new NotImplementedException("SHA-224 not implemented.");	// TODO
	}
}
