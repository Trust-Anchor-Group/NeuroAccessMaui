using System;
using System.Security.Cryptography.X509Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
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
		public static bool VerifySignatures(params X509Certificate2[] Certificates)
		{
			return VerifySignatures(null, Certificates);
		}

		/// <summary>
		/// Verifies the signatures of a chain of ICAO certificates.
		/// </summary>
		/// <param name="Certificates">Sequence of certificates, from CA, through intermediate issuers,
		/// to leaf certificate.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If signatures in chain are valid.</returns>
		public static bool VerifySignatures(ICommunicationLayer? Client, params X509Certificate2[] Certificates)
		{
			if (Certificates.Length == 0)
				return false;

			X509Certificate2 Issuer = Certificates[0];  // Root is self-signed.
			int Index = 0;

			foreach (X509Certificate2 Certificate in Certificates)
			{
				Client?.Information("Validating certificate " + (++Index) + " signature.");

				if (!TravelDocumentsClient.TryDecodeDER(Certificate.RawData, out object? Obj) ||
					Obj is not Vector CertificateVector ||
					CertificateVector.Length != 3 ||
					CertificateVector[0] is not Vector TbsCertificate ||
					CertificateVector[1] is not Vector AlgorithmIdentifier ||
					CertificateVector[2] is not byte[] Signature)
				{
					Client?.Error("Unable to decode certificate.\r\n\r\n" +
						Convert.ToBase64String(Certificate.RawData, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				ISignatureAlgorithm? SignatureAlgorithm = Security.SignatureAlgorithms.SignatureAlgorithm.TryDecode(AlgorithmIdentifier, Client);
				if (SignatureAlgorithm is null)
				{
					Client?.Error("Certificate signature algorithm not found or supported.\r\n\r\n" +
						Convert.ToBase64String(Certificate.RawData, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				if (!SignatureAlgorithm.VerifySignature(TbsCertificate.SubSection, Signature,
					Issuer, Client))
				{
					Client?.Error("Certificate signature not valid.\r\n\r\n" +
						Convert.ToBase64String(Certificate.RawData, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				Issuer = Certificate;
			}

			return true;
		}
	}
}
