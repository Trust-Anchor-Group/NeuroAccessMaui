using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
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
		/// Tries to parse an ASN.1-encoded Certificate List, as defined in
		/// RFC 5280, §5.1
		/// </summary>
		/// <param name="Crl">Decoded CRL.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(Vector Crl, [NotNullWhen(true)] out CertificateList? Parsed)
		{
			Parsed = null;

			if (Crl.Elements.Length != 3)
				return false;

			if (Crl.Elements.GetValue(0) is not Vector TbsCertList)
				return false;

			if (Crl.Elements.GetValue(1) is not Vector AlgorithmIdentifier)
				return false;

			ISignatureAlgorithm? SignatureAlgorithm = Security.SignatureAlgorithms.SignatureAlgorithm.TryDecode(AlgorithmIdentifier);
			if (SignatureAlgorithm is null)
				return false;

			if (Crl.Elements.GetValue(2) is not byte[] Signature)
				return false;

			if (!ToBeSignedCertificateList.TryParse(TbsCertList, out ToBeSignedCertificateList? ToBeSigned))
				return false;

			Parsed = new CertificateList(Crl, ToBeSigned, SignatureAlgorithm, Signature);

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
		/// Checks if a certificate has been revoked.
		/// </summary>
		/// <param name="Certificate">Certificate</param>
		/// <param name="Reason">Reason for the certificate being revoked.</param>
		/// <returns>If the certificate has been revoked.</returns>
		public bool HasBeenRevoked(X509Certificate2 Certificate, out RevokedReason Reason)
		{
			return this.ToBeSignedCertificateList.HasBeenRevoked(Certificate, out Reason);
		}

		/// <summary>
		/// Verifies the signature of the CRL
		/// </summary>
		/// <param name="CountryCode">Country Code of issuer</param>
		/// <returns>If the signature is valid.</returns>
		public Task<bool> VerifySignature(string CountryCode)
		{
			return this.VerifySignature(CountryCode, null);
		}

		/// <summary>
		/// Verifies the signature of the CRL
		/// </summary>
		/// <param name="CountryCode">Country Code of issuer</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the signature is valid.</returns>
		public async Task<bool> VerifySignature(string CountryCode, ICommunicationLayer? Client)
		{
			if (this.Signature is null)
			{
				Client?.Error("No signature in CRL.");
				return false;
			}

			if (this.ToBeSignedCertificateList?.AuthorityKeyIdentifier is null)
			{
				Client?.Error("No AKI in CRL.");
				return false;
			}

			X509Certificate2? SignerCertificate = await CertificateStore.TryLoadCertificate(
				"id.tagroot.io", CountryCode, this.ToBeSignedCertificateList.AuthorityKeyIdentifier,
				Client);

			if (SignerCertificate is null)
				return false;

			return this.SignatureAlgorithm.VerifySignature(this.ToBeSignedCertificateList.Binary,
				this.Signature, SignerCertificate);
		}

	}
}
