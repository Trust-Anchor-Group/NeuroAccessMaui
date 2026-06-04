namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.DistinguishedNames
{
	/// <summary>
	/// EMail Address
	/// </summary>
	public class ContentType : SecurityString
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.9.3";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length == 2)
			{
				object? Value = SecurityInfo[1];

				if (Value is null)
					this.Value = null;
				else if (Value is ISecurityObject SecurityObject)
					this.Value = SecurityObject.Oid;
				else if (Value is string StringValue)
					this.Value = StringValue;
				else if (Value is Vector v && v.Length == 1)
				{
					if (v.FirstElement is ISecurityObject SecurityObject2)
						this.Value = SecurityObject2.Oid;
					else if (v.FirstElement is string StringValu2)
						this.Value = StringValu2;
					else
						return false;
				}
				else
					return false;

				this.configured = true;

				return true;
			}
			else
				return false;
		}
	}
}
