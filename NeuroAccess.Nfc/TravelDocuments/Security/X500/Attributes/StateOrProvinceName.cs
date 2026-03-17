namespace NeuroAccess.Nfc.TravelDocuments.Security.X500.Attributes
{
	/// <summary>
	/// State or Province Name.
	/// </summary>
	public class StateOrProvinceName : SecurityString
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.4.8";
	}
}
