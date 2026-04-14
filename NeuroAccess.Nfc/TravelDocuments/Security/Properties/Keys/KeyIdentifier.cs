namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.Keys
{
	public abstract class KeyIdentifier : GeneralName
	{
		private byte[]? identifier;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (!base.Configure(SecurityInfo))
				return false;

			if (this.Name is not byte[] Identifier)
				return false;

			this.identifier = Identifier;

			return true;
		}

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.identifier is not null;

		/// <summary>
		/// Key identifier
		/// </summary>
		public byte[] Identifier => this.identifier ?? [];
	}
}
