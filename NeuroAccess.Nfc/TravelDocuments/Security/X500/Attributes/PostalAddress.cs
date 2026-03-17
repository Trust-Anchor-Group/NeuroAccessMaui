namespace NeuroAccess.Nfc.TravelDocuments.Security.X500.Attributes
{
	/// <summary>
	/// Postal Address.
	/// </summary>
	public class PostalAddress : SecurityString
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.4.16";
	}
}
