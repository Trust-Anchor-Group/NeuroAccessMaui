namespace NeuroAccess.Nfc.TravelDocuments.Security.X500.Certificates
{
	/// <summary>
	/// Issuer Alternative Name.
	/// </summary>
	public class IssuerAlternativeName : SecurityBinary
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.18";
	}
}
