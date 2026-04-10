using System.Formats.Asn1;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// Authority Key Identifier
	/// </summary>
	public class AuthorityKeyIdentifier : SecurityBinary
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.35";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (!base.Configure(SecurityInfo))
				return false;

			if (!ASN1.TryDecodeDerAs(UniversalTagNumber.OctetString, this.Value, out object? ParsedExtension))
				return false;

			if (ParsedExtension is not byte[] Identifier)
				return false;

			this.value = Identifier;

			return true;
		}
	}
}
