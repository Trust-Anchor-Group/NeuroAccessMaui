namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// Subject Alternative Name
	/// </summary>
	public class SubjectAltName : GeneralName
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.17";

		public override bool Configure(Vector SecurityInfo)
		{
			if (!base.Configure(SecurityInfo))
				return false;

			return true;
		}
	}
}
