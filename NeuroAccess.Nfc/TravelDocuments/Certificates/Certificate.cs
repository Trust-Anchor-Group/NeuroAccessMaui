using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.Certificates
{
	/// <summary>
	/// Certificate, as defined in RFC 5280, §4.1.
	/// </summary>
	public class Certificate
	{
		/// <summary>
		/// Certificate, as defined in RFC 5280, §4.1.
		/// </summary>
		/// <param name="Asn1Vector">ASN.1 decoded vector.</param>
		/// <param name="ToBeSignedCertificate">Certificate that is signed.</param>
		/// <param name="SignatureAlgorithm">Algorithm used to sign the certificate.</param>
		/// <param name="Signature">Digital signature.</param>
		private Certificate(Vector Asn1Vector, ToBeSignedCertificate ToBeSignedCertificate,
			ISignatureAlgorithm SignatureAlgorithm, byte[] Signature)
		{
			this.Asn1Vector = Asn1Vector;
			this.ToBeSignedCertificate = ToBeSignedCertificate;
			this.SignatureAlgorithm = SignatureAlgorithm;
			this.Signature = Signature;
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Certificate, as defined in RFC 5280, §4.1.
		/// </summary>
		/// <param name="RawCertificate">ASN.1 DER encoded certificate.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(byte[] RawCertificate, [NotNullWhen(true)] out Certificate? Parsed)
		{
			Parsed = null;

			if (!ASN1.TryDecodeDer(RawCertificate, out object? Content))
				return false;

			if (Content is not Vector CertificateVector)
				return false;

			return TryParse(CertificateVector, out Parsed);
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Certificate, as defined in RFC 5280, §4.1.
		/// </summary>
		/// <param name="CertificateVector">Decoded Certificate Vector.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(Vector CertificateVector, [NotNullWhen(true)] out Certificate? Parsed)
		{
			Parsed = null;

			if (CertificateVector.Length != 3)
				return false;

			if (CertificateVector[0] is not Vector TbsCert)
				return false;

			if (CertificateVector[1] is not ISignatureAlgorithm SignatureAlgorithm)
			{
				if (CertificateVector[1] is not Vector AlgorithmIdentifier)
					return false;

				ISignatureAlgorithm? SignatureAlgorithm2 = Security.SignatureAlgorithms.SignatureAlgorithm.TryDecode(AlgorithmIdentifier);
				if (SignatureAlgorithm2 is null)
					return false;

				SignatureAlgorithm = SignatureAlgorithm2;
			}

			if (CertificateVector[2] is not byte[] Signature)
				return false;

			if (!ToBeSignedCertificate.TryParse(TbsCert, out ToBeSignedCertificate? ToBeSigned))
				return false;

			Parsed = new Certificate(CertificateVector, ToBeSigned, SignatureAlgorithm, Signature);

			return true;
		}

		/// <summary>
		/// Certificate that is signed.
		/// </summary>
		public ToBeSignedCertificate ToBeSignedCertificate { get; }

		/// <summary>
		/// Algorithm used to sign the certificate.
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
		/// ASN.1 DER encoded certificate.
		/// </summary>
		public byte[] Binary => this.Asn1Vector.SubSection;

		/// <summary>
		/// Version of document.
		/// </summary>
		public int? Version => this.ToBeSignedCertificate.Version;

		/// <summary>
		/// Serial Number
		/// </summary>
		public System.Numerics.BigInteger SerialNumber => this.ToBeSignedCertificate.SerialNumber;

		/// <summary>
		/// Issuer
		/// </summary>
		public Names Issuer => this.ToBeSignedCertificate.Issuer;

		/// <summary>
		/// Signatures cannot be created before this timestamp.
		/// </summary>
		public DateTimeOffset NotBefore => this.ToBeSignedCertificate.NotBefore;

		/// <summary>
		/// Signatures cannot be created after this timestamp.
		/// </summary>
		public DateTimeOffset NotAfter => this.ToBeSignedCertificate.NotAfter;

		/// <summary>
		/// Subject
		/// </summary>
		public Names Subject => this.ToBeSignedCertificate.Subject;

		/// <summary>
		/// Authority Key Identifier
		/// </summary>
		public byte[]? AuthorityKeyIdentifier => this.ToBeSignedCertificate.AuthorityKeyIdentifier;

		/// <summary>
		/// Subject Key Identifier
		/// </summary>
		public byte[]? SubjectKeyIdentifier => this.ToBeSignedCertificate.SubjectKeyIdentifier;

		/// <summary>
		/// Extensions
		/// </summary>
		public Vector? Extensions => this.ToBeSignedCertificate.Extensions;

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
				return false;

			return this.SignatureAlgorithm.VerifySignature(this.Binary, this.Signature,
				SignerCertificate, Client);
		}

	}
}
