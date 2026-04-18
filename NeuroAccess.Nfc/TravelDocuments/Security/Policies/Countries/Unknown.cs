namespace NeuroAccess.Nfc.TravelDocuments.Security.Policies.Countries
{
	/// <summary>
	/// National/private OID under arc 1.2.256; exact owner/child meaning not publicly documented
	/// in the sources checked.
	/// </summary>
	public class Unknown : PolicyObject
	{
		public override string Oid => "1.2.256.517.2.10.5";
	}
}
