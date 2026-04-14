namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// Issuer Alternative Name
	/// </summary>
	public class IssuerAltName : GeneralName
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.18";
	}
}
