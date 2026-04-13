using System;
using Waher.Security;

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
		/// Hash algorithm to use for in-memory blocks of data.
		/// </summary>
		public override HashFunctionArray HashAlgorithm => 
			throw new NotImplementedException("SHA-224 not implemented.");  // TODO

		/// <summary>
		/// Hash algorithm to use for streams of data.
		/// </summary>
		public override HashFunctionStream HashAlgorithmStream =>
			throw new NotImplementedException("SHA-224 not implemented.");  // TODO
	}
}
