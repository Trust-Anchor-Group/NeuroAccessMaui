using System;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.Certificates
{
	/// <summary>
	/// Static class for validation of ICAO certificate chains
	/// </summary>
	public static class CertificateChain
	{
		/// <summary>
		/// Verifies the signatures of a chain of ICAO certificates.
		/// </summary>
		/// <param name="Certificates">Sequence of certificates, from CA, through intermediate issuers,
		/// to leaf certificate.</param>
		/// <returns>If signatures in chain are valid.</returns>
		public static bool VerifySignatures(params Certificate[] Certificates)
		{
			return VerifySignatures(null, Certificates);
		}

		/// <summary>
		/// Verifies the signatures of a chain of ICAO certificates.
		/// </summary>
		/// <param name="Client">Optional client reference.</param>
		/// <param name="Certificates">Sequence of certificates, from CA, through intermediate issuers,
		/// to leaf certificate.</param>
		/// <returns>If signatures in chain are valid.</returns>
		public static bool VerifySignatures(ICommunicationLayer? Client, params Certificate[] Certificates)
		{
			if (Certificates.Length == 0)
				return false;

			Certificate Issuer = Certificates[0];  // Root is self-signed.
			int Index = 0;

			foreach (Certificate Cert in Certificates)
			{
				Client?.Information("Validating certificate " + (++Index) + " signature.");

				if (Cert.SignatureAlgorithm is null)
				{
					Client?.Error("Unable to decode signature algorithm.\r\n\r\n" +
						Convert.ToBase64String(Cert.Binary, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				if (!Cert.SignatureAlgorithm.IsConfigured)
				{
					Client?.Error("Signature algorithm not configured properly.\r\n\r\n" +
						Convert.ToBase64String(Cert.Binary, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				if (!Cert.SignatureAlgorithm.VerifySignature(Cert.Binary, Cert.Signature,
					Issuer, Client))
				{
					Client?.Error("Certificate signature not valid.\r\n\r\n" +
						Convert.ToBase64String(Cert.Binary, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				Issuer = Cert;
			}

			return true;
		}
	}
}
