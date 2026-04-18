namespace NeuroAccess.Nfc.TravelDocuments.Security.Policies
{
	/// <summary>
	/// Abstract base class for policy objects.
	/// </summary>
	public abstract class PolicyObject : SecurityObject
	{
		private Vector? configuration;

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.configuration is not null;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			this.configuration = SecurityInfo;
			return true;
		}
	}
}
