namespace NeuroAccess.Nfc.TravelDocuments.Security.Policies
{
	/// <summary>
	/// Entrust/Nortel private attribute under the Secure Networks attributes arc; exact
	/// child meaning not publicly documented in sources checked.
	/// </summary>
	public class EntrustNortel : PolicyObject
	{
		public override string Oid => "1.2.840.113533.7.68.29";
	}
}
