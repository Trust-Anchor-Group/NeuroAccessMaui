namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// Certificate Policies
	/// </summary>
	public class CertificatePolicies : SecurityObject
	{
		private Vector? certificatePolicies;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.32";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.LastElement is not Vector CertificatePolicies)
				return false;

			this.certificatePolicies= CertificatePolicies;

			return true;
		}
	}
}
