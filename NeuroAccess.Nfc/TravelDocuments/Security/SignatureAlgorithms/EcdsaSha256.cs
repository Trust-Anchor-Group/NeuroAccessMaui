using System;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// SHA-256 with ECDSA
	/// </summary>
	public class EcdsaSha256 : EcdsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.10045.4.3.2";

		/// <summary>
		/// Hash algorithm to use for in-memory blocks of data.
		/// </summary>
		public override HashFunctionArray HashAlgorithm => Hashes.ComputeSHA256Hash;

		/// <summary>
		/// Hash algorithm to use for streams of data.
		/// </summary>
		public override HashFunctionStream HashAlgorithmStream => Hashes.ComputeSHA256Hash;
	}
}
