namespace NeuroAccess.Nfc.TravelDocuments.Security.X500.Attributes
{
	/// <summary>
	/// Organization Name.
	/// </summary>
	public class OrganizationUnitName : SecurityString
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.4.11";
	}
}
