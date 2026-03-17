namespace NeuroAccess.Nfc.TravelDocuments.Security.X500.Attributes
{
	/// <summary>
	/// Common Name.
	/// </summary>
	public class CommonName : SecurityString
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.4.3";
	}
}
