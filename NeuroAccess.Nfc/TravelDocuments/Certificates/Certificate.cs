using System;
using System.Diagnostics.CodeAnalysis;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;

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
			ISignatureAlgorithm IssuerSignatureAlgorithm, byte[] Signature)
		{
			this.Asn1Vector = Asn1Vector;
			this.ToBeSignedCertificate = ToBeSignedCertificate;
			this.IssuerSignatureAlgorithm = IssuerSignatureAlgorithm;
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

			if (CertificateVector[1] is not ISignatureAlgorithm IssuerSignatureAlgorithm)
			{
				if (CertificateVector[1] is not Vector AlgorithmIdentifier)
					return false;

				ISignatureAlgorithm? IssuerSignatureAlgorithm2 = SignatureAlgorithm.TryDecode(AlgorithmIdentifier);
				if (IssuerSignatureAlgorithm2 is null)
					return false;

				IssuerSignatureAlgorithm = IssuerSignatureAlgorithm2;
			}

			if (CertificateVector[2] is not byte[] Signature)
				return false;

			if (!ToBeSignedCertificate.TryParse(TbsCert, out ToBeSignedCertificate? ToBeSigned))
				return false;

			Parsed = new Certificate(CertificateVector, ToBeSigned, IssuerSignatureAlgorithm, Signature);

			return true;
		}

		/// <summary>
		/// Certificate that is signed.
		/// </summary>
		public ToBeSignedCertificate ToBeSignedCertificate { get; }

		/// <summary>
		/// Signature algorithm used by issuer to sign the certificate.
		/// </summary>
		public ISignatureAlgorithm IssuerSignatureAlgorithm { get; }

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
		/// Certificate public key, used to verify signatures issued by the certificate.
		/// </summary>
		public IPublicKey PublicKey => this.ToBeSignedCertificate.PublicKey;

		/// <summary>
		/// Extensions
		/// </summary>
		public Vector? Extensions => this.ToBeSignedCertificate.Extensions;
	}
}
