using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Networking;
using Waher.Runtime.Collections;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Certificates
{
	/// <summary>
	/// Static class for validation of ICAO certificate chains
	/// </summary>
	public static class CertificateChain
	{
		/// <summary>
		/// Gets the certificate chain for a given certificate.
		/// </summary>
		/// <param name="Certificate">Certificate to validate.</param>
		/// <param name="CertificateHost">Host of trusted certificates.</param>
		/// <returns>Certificate Chain</returns>
		public static Task<Certificate[]> GetChain(Certificate Certificate, string CertificateHost)
		{
			return GetChain(Certificate, [], CertificateHost, null);
		}

		/// <summary>
		/// Gets the certificate chain for a given certificate.
		/// </summary>
		/// <param name="Certificate">Certificate to validate.</param>
		/// <param name="CertificateHost">Host of trusted certificates.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>Certificate Chain</returns>
		public static Task<Certificate[]> GetChain(Certificate Certificate, string CertificateHost,
			ICommunicationLayer? Client)
		{
			return GetChain(Certificate, [], CertificateHost, Client);
		}

		/// <summary>
		/// Gets the certificate chain for a given certificate, as well as the URLs for associated
		/// Certificate Revocation Lists.
		/// </summary>
		/// <param name="Certificate">Certificate to validate.</param>
		/// <param name="CertificateRevocationLists">URLs to associated Certificate Revocation Lists.</param>
		/// <param name="CertificateHost">Host of trusted certificates.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>Certificate Chain</returns>
		public static async Task<Certificate[]> GetChain(Certificate Certificate,
			Dictionary<string, bool> CertificateRevocationLists, string CertificateHost,
			ICommunicationLayer? Client)
		{
			ChunkedList<Certificate> Certificates = [Certificate];

			foreach (string CrlUrl in TravelDocumentsClient.GetRevocationListUrls(Certificate))
				CertificateRevocationLists[CrlUrl] = true;

			KeyValuePair<string?, byte[]?> P = TravelDocumentsClient.GetAuthorityKeyIdentifier(Certificate);
			KeyValuePair<string?, byte[]?> P2 = TravelDocumentsClient.GetSubjectKeyIdentifier(Certificate);

			if (P.Value is null || (AreEquals(P.Value, P2.Value) && P.Key == P2.Key))
				return [.. Certificates];

			Dictionary<string, bool> Processed = [];
			string? CountryCode = P.Key;
			byte[]? IssuerKeyReference = P.Value;

			while (!string.IsNullOrEmpty(CountryCode) && IssuerKeyReference is not null)
			{
				string Key = Convert.ToBase64String(IssuerKeyReference);
				if (Processed.ContainsKey(Key))
					break;

				Processed[Key] = true;

				Certificate? IssuerCertificate = await CertificateStore.TryLoadCertificate(
					CertificateHost, CountryCode, IssuerKeyReference, Client);

				if (IssuerCertificate is null)
				{
					Client?.Error("Issuer certificate not found: " + CountryCode + ", " +
						Hashes.BinaryToString(IssuerKeyReference));
					break;
				}

				Certificates.Insert(0, IssuerCertificate);

				foreach (string CrlUrl in TravelDocumentsClient.GetRevocationListUrls(IssuerCertificate))
					CertificateRevocationLists[CrlUrl] = true;

				P = TravelDocumentsClient.GetAuthorityKeyIdentifier(IssuerCertificate);
				CountryCode = P.Key;
				IssuerKeyReference = P.Value;
			}

			return [.. Certificates];
		}

		private static bool AreEquals(byte[]? A1, byte[]? A2)
		{
			if ((A1 is null) ^ (A2 is null))
				return false;

			if (A1 is null)
				return true;

			int c = A1.Length;
			if (A2!.Length != c)
				return false;

			for (int i = 0; i < c; i++)
			{
				if (A1[i] != A2[i])
					return false;
			}

			return true;
		}

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
				Client?.Information("Validating certificate " + (++Index) + " signature, serial number: " + Cert.SerialNumber.ToString(CultureInfo.InvariantCulture));

				if (Issuer.PublicKey is null)
				{
					Client?.Error("Unable to decode issuer signature algorithm and public key.\r\n\r\n" +
						Convert.ToBase64String(Cert.Binary, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				if (Issuer.PublicKey is ISecurityObject PublicKeyObject &&
					!PublicKeyObject.IsConfigured)
				{
					Client?.Error("Issuer public key not configured properly.\r\n\r\n" +
						Convert.ToBase64String(Cert.Binary, Base64FormattingOptions.InsertLineBreaks));

					return false;
				}

				if (!Cert.IssuerSignatureAlgorithm.VerifySignature(Cert.ToBeSignedCertificate.Binary,
					Cert.Signature, Issuer.PublicKey, Client))
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
