using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// Private Key Usage Period
	/// </summary>
	public class PrivateKeyUsagePeriod : SecurityObject
	{
		private DateTimeOffset? notBefore = null;
		private DateTimeOffset? notAfter = null;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.5.29.16";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.LastElement is not Vector UsagePeriod)
				return false;

			int c = UsagePeriod.Length;

			if (c > 0)
			{
				if (UsagePeriod[0] is not DateTimeOffset NotBefore)
					return false;

				this.notBefore = NotBefore;

				if (c > 1)
				{
					if (UsagePeriod[1] is not DateTimeOffset NotAfter)
						return false;

					this.notAfter = NotAfter;
				}
			}

			return true;
		}

		/// <summary>
		/// Not Before
		/// </summary>
		public DateTimeOffset? NotBefore => this.notBefore;

		/// <summary>
		/// Not After
		/// </summary>
		public DateTimeOffset? NotAfter => this.notAfter;
	}
}
