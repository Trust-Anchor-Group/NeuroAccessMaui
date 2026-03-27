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
	}
}
