using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using Waher.Content;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.RevocationLists
{
	/// <summary>
	/// Certificate List, as defined in RFC 5280, §5.1
	/// </summary>
	public class CertificateList
	{
		/// <summary>
		/// Certificate List, as defined in RFC 5280, §5.1
		/// </summary>
		/// <param name="Asn1Vector">ASN.1 decoded vector.</param>
		/// <param name="ToBeSignedCertificateList">List of certificates that is signed.</param>
		/// <param name="SignatureAlgorithm">Algorithm used to sign the certificate list.</param>
		/// <param name="Signature">Digital signature.</param>
		private CertificateList(Vector Asn1Vector, ToBeSignedCertificateList ToBeSignedCertificateList,
			ISignatureAlgorithm SignatureAlgorithm, byte[] Signature)
		{
			this.Asn1Vector = Asn1Vector;
			this.ToBeSignedCertificateList = ToBeSignedCertificateList;
			this.SignatureAlgorithm = SignatureAlgorithm;
			this.Signature = Signature;
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Certificate List, as defined in RFC 5280, §5.1.
		/// </summary>
		/// <param name="RawCertificateLIst">ASN.1 DER encoded certificate list.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(byte[] RawCertificateList, [NotNullWhen(true)] out CertificateList? Parsed)
		{
			Parsed = null;

			if (!ASN1.TryDecodeDer(RawCertificateList, out object? Content))
				return false;

			if (Content is not Vector CertificateListVector)
				return false;

			return TryParse(CertificateListVector, out Parsed);
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Certificate List, as defined in
		/// RFC 5280, §5.1
		/// </summary>
		/// <param name="CertificateListVector">Decoded Certificate List Vector.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(Vector CertificateListVector, [NotNullWhen(true)] out CertificateList? Parsed)
		{
			Parsed = null;

			if (CertificateListVector.Length != 3)
				return false;

			if (CertificateListVector.FirstElement is not Vector TbsCertList)
				return false;

			if (CertificateListVector[1] is not Vector AlgorithmIdentifier)
				return false;

			ISignatureAlgorithm? SignatureAlgorithm = Security.SignatureAlgorithms.SignatureAlgorithm.TryDecode(AlgorithmIdentifier);
			if (SignatureAlgorithm is null)
				return false;

			if (CertificateListVector[2] is not byte[] Signature)
				return false;

			if (!ToBeSignedCertificateList.TryParse(TbsCertList, out ToBeSignedCertificateList? ToBeSigned))
				return false;

			Parsed = new CertificateList(CertificateListVector, ToBeSigned, SignatureAlgorithm, Signature);

			return true;
		}

		/// <summary>
		/// List of certificates that is signed.
		/// </summary>
		public ToBeSignedCertificateList ToBeSignedCertificateList { get; }

		/// <summary>
		/// Algorithm used to sign the certificate list.
		/// </summary>
		public ISignatureAlgorithm SignatureAlgorithm { get; }

		/// <summary>
		/// Digital signature.
		/// </summary>
		public byte[] Signature { get; }

		/// <summary>
		/// ASN.1 decoded vector
		/// </summary>
		public Vector Asn1Vector { get; }

		/// <summary>
		/// Version of document.
		/// </summary>
		public int? Version => this.ToBeSignedCertificateList.Version;

		/// <summary>
		/// Issuer
		/// </summary>
		public Vector Issuer => this.ToBeSignedCertificateList.Issuer;

		/// <summary>
		/// This update
		/// </summary>
		public DateTimeOffset ThisUpdate => this.ToBeSignedCertificateList.ThisUpdate;

		/// <summary>
		/// Next update, or <see cref="DateTime.MaxValue"/> if not specified.
		/// </summary>
		public DateTimeOffset NextUpdate => this.ToBeSignedCertificateList.NextUpdate;

		/// <summary>
		/// List of revoked certificates.
		/// </summary>
		public RevokedCertificate[] RevokedCertificates => this.ToBeSignedCertificateList.RevokedCertificates;

		/// <summary>
		/// Authority Key Identifier
		/// </summary>
		public byte[]? AuthorityKeyIdentifier => this.ToBeSignedCertificateList.AuthorityKeyIdentifier;

		/// <summary>
		/// Extensions
		/// </summary>
		public Vector? Extensions => this.ToBeSignedCertificateList.Extensions;

		/// <summary>
		/// Checks if a certificate has been revoked.
		/// </summary>
		/// <param name="Certificate">Certificate</param>
		/// <param name="Reason">Reason for the certificate being revoked.</param>
		/// <returns>If the certificate has been revoked.</returns>
		public bool HasBeenRevoked(Certificate Certificate, out RevokedReason Reason)
		{
			return this.ToBeSignedCertificateList.HasBeenRevoked(Certificate, out Reason);
		}

		/// <summary>
		/// Verifies the signature of the CRL
		/// </summary>
		/// <param name="IdDomain">Domain name of Neuron hosting ICAO certificates.</param>
		/// <param name="CountryCode">Country Code of issuer</param>
		/// <returns>If the signature is valid.</returns>
		public Task<bool> VerifySignature(string IdDomain, string CountryCode)
		{
			return this.VerifySignature(IdDomain, CountryCode, null);
		}

		/// <summary>
		/// Verifies the signature of the CRL
		/// </summary>
		/// <param name="IdDomain">Domain name of Neuron hosting ICAO certificates.</param>
		/// <param name="CountryCode">Country Code of issuer</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the signature is valid.</returns>
		public async Task<bool> VerifySignature(string IdDomain, string CountryCode,
			ICommunicationLayer? Client)
		{
			if (this.Signature is null)
			{
				Client?.Error("No signature in CRL.");
				return false;
			}

			if (this.AuthorityKeyIdentifier is null)
			{
				Client?.Error("No AKI in CRL.");
				return false;
			}

			Certificate? SignerCertificate = await CertificateStore.TryLoadCertificate(
				IdDomain, CountryCode, this.AuthorityKeyIdentifier, Client);

			if (SignerCertificate is null)
			{
				Client?.Error("Unable to load issuer certificate from the AKI: " +
					Waher.Security.Hashes.BinaryToString(this.AuthorityKeyIdentifier));
				return false;
			}

			if (SignerCertificate.PublicKey is null)
			{
				Client?.Error("Unable to decode public key from issuer certificate.\r\n\r\n" +
					Convert.ToBase64String(SignerCertificate.Binary, Base64FormattingOptions.InsertLineBreaks));
				return false;
			}

			if (this.SignatureAlgorithm.VerifySignature(this.ToBeSignedCertificateList.Binary,
				this.Signature, SignerCertificate.PublicKey, Client))
			{
				return true;
			}

			if (Client?.HasSniffers ?? false)
			{
				StringBuilder sb = new();

				sb.AppendLine("CRL signature verification failed.");
				sb.AppendLine();
				sb.AppendLine("Data to be signed:");
				sb.AppendLine(Convert.ToBase64String(this.ToBeSignedCertificateList.Binary,
					Base64FormattingOptions.InsertLineBreaks));
				sb.AppendLine();
				sb.AppendLine("Signature:");
				sb.AppendLine(Convert.ToBase64String(this.Signature,
					Base64FormattingOptions.InsertLineBreaks));
				sb.AppendLine();
				sb.AppendLine("Authority Key Identifier:");
				sb.AppendLine(Waher.Security.Hashes.BinaryToString(this.AuthorityKeyIdentifier));
				sb.AppendLine();
				sb.AppendLine("Public Key to verify signature:");
				sb.AppendLine(JSON.Encode(SignerCertificate.PublicKey, true));

				Client.Warning(sb.ToString());
			}

			return false;
		}

	}
}
