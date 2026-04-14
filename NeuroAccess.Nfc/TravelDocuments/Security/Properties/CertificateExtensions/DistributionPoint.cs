using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions
{
	/// <summary>
	/// CRL Distribution Point
	/// </summary>
	public class DistributionPoint
	{
		private string url = string.Empty;
		private Vector? reasons = null;
		private Vector? crlIssuer = null;

		/// <summary>
		/// Tries to create a Distribution Point object instance from its ASN.1 decoded representation.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <param name="Result">Result, if successful.</param>
		/// <returns>If the object could be decoded and created.</returns>
		public static bool TryCreate(Vector SecurityInfo, [NotNullWhen(true)] out DistributionPoint? Result)
		{
			Result = new DistributionPoint();

			int i = 0;
			int c = SecurityInfo.Length;

			if (i < c && SecurityInfo[i] is Vector DistributionPointName)
			{
				i++;

				if (!GeneralName.TryParse(DistributionPointName, out object? Name) ||
					Name is not string Url)
				{
					return false;
				}

				Result.url = Url;
			}

			if (i < c && SecurityInfo[i] is Vector Reasons)
			{
				i++;
				Result.reasons = Reasons;
			}

			if (i < c && SecurityInfo[i] is Vector CrlIssuer)
			{
				i++;
				Result.crlIssuer = CrlIssuer;
			}

			if (i < c)
			{
				Result = null;
				return false;
			}
			else
				return true;
		}

		/// <summary>
		/// URL to CRL distribution point.
		/// </summary>
		public string Url => this.url;
	}
}
