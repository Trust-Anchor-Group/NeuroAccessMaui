using System;
using System.Security.Cryptography;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// RSASSA-PSS algorithm, as defined in RFCs 3447, 4055 and 4056:
	/// https://www.rfc-editor.org/rfc/rfc3447
	/// https://www.rfc-editor.org/rfc/rfc4055
	/// https://www.rfc-editor.org/rfc/rfc4056
	/// </summary>
	public class RsaPss : RsaAlgorithm
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.10";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length < 2 ||
				SecurityInfo[1] is not Vector RsaSsaPssParameters)
			{
				return false;
			}

			return false;   // TODO: Implement configuration of RSASSA-PSS parameters, as defined in RFC 4055, §3.1
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override HashAlgorithmName HashAlgorithmName =>
			throw new NotImplementedException("RSASSA-PSS not implemented.");  // TODO
	}
}
