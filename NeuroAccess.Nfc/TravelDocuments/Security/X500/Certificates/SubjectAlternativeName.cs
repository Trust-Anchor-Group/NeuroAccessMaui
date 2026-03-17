namespace NeuroAccess.Nfc.TravelDocuments.Security.X500.Certificates
{
	/// <summary>
	/// Subject Alternative Name.
	/// </summary>
	public class SubjectAlternativeName : SecurityBinary
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.17";
	}
}
