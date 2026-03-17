namespace NeuroAccess.Nfc.TravelDocuments.Security.X500.Attributes
{
	/// <summary>
	/// Street Address.
	/// </summary>
	public class StreetAddress : SecurityString
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.4.9";
	}
}
