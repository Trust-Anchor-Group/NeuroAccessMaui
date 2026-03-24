using System;
using System.Security.Cryptography;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-224 with ECDSA
	/// </summary>
	public class EcdsaSha224 : EcdsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.10045.4.3.1";

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashAlgorithmName HashAlgorithmName =>
			throw new NotImplementedException("SHA-224 not implemented.");  // TODO
	}
}
