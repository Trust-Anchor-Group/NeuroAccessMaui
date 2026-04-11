using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions;
using NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using System;
using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.Certificates
{
	/// <summary>
	/// Certificate, without signature, as defined in RFC 5280, §4.1
	/// </summary>
	public class ToBeSignedCertificate
	{
		/// <summary>
		/// Certificate, without signature, as defined in RFC 5280, §4.1
		/// </summary>
		/// <param name="Binary">Binary representation of certificate to be signed.</param>
		/// <param name="Version">Version of representation.</param>
		/// <param name="SerialNumber">Serial number</param>
		/// <param name="IssuerSignatureAlgorithm">Issuer Signature algorithm used to sign the certificate.</param>
		/// <param name="Issuer">Information about Issuer.</param>
		/// <param name="NotBefore">Signatures cannot be created before this timestamp.</param>
		/// <param name="NotAfter">Signatures cannot be created after this timestamp.</param>
		/// <param name="Subject">Information about Subject.</param>
		/// <param name="AuthorityKeyIdentifier">Authority Key Identifier, if known.</param>
		/// <param name="SubjectKeyIdentifier">Subject Key Identifier, if known.</param>
		/// <param name="CertificatePublicKey">Certificate public key, used to verify signatures issued by the certificate.</param>
		/// <param name="Extensions">Extensions, if any.</param>
		private ToBeSignedCertificate(byte[] Binary, int Version, System.Numerics.BigInteger SerialNumber,
			ISignatureAlgorithm IssuerSignatureAlgorithm, Names Issuer, DateTimeOffset NotBefore,
			DateTimeOffset NotAfter, Names Subject, byte[]? AuthorityKeyIdentifier,
			byte[]? SubjectKeyIdentifier, IPublicKey CertificatePublicKey, Vector? Extensions)
		{
			this.Binary = Binary;
			this.Version = Version;
			this.SerialNumber = SerialNumber;
			this.IssuerSignatureAlgorithm = IssuerSignatureAlgorithm;
			this.Issuer = Issuer;
			this.NotBefore = NotBefore;
			this.NotAfter = NotAfter;
			this.Subject = Subject;
			this.AuthorityKeyIdentifier = AuthorityKeyIdentifier;
			this.SubjectKeyIdentifier = SubjectKeyIdentifier;
			this.PublicKey = CertificatePublicKey;
			this.Extensions = Extensions;
		}

		/// <summary>
		/// Tries to parse an ASN.1-encoded Certificate, as defined in RFC 5280, §4.1
		/// </summary>
		/// <param name="TbsCert">Decoded elements of the object.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(Vector TbsCert, [NotNullWhen(true)] out ToBeSignedCertificate? Parsed)
		{
			Parsed = null;

			int c = TbsCert.Length;
			int i = 0;
			int Version = 0;

			if (i < c)
			{
				if (TbsCert[0] is not System.Numerics.BigInteger V)
				{
					if (TbsCert[0] is not Vector VersionVector ||
						VersionVector.Length != 1 ||
						VersionVector.FirstElement is not System.Numerics.BigInteger V2)
					{
						return false;
					}

					V = V2;
				}

				if (V < int.MinValue || V > int.MaxValue)
					return false;

				Version = (int)V;
				i++;
			}

			if (i >= c || TbsCert[i++] is not System.Numerics.BigInteger SerialNumber)
				return false;

			if (i >= c)
				return false;

			object? Obj = TbsCert[i++];

			if (Obj is not ISignatureAlgorithm IssuerSignatureAlgorithm)
			{
				if (Obj is not Vector AlgorithmIdentifier)
					return false;

				ISignatureAlgorithm? IssuerSignatureAlgorithm2 = SignatureAlgorithm.TryDecode(AlgorithmIdentifier);
				if (IssuerSignatureAlgorithm2 is null)
					return false;

				IssuerSignatureAlgorithm = IssuerSignatureAlgorithm2;
			}

			if (i >= c || TbsCert[i++] is not Vector Issuer)
				return false;

			if (i >= c ||
				TbsCert[i++] is not Vector Validity ||
				Validity.Length != 2 ||
				Validity.FirstElement is not DateTimeOffset NotBefore ||
				Validity.LastElement is not DateTimeOffset NotAfter)
			{
				return false;
			}

			if (i >= c || TbsCert[i++] is not Vector Subject)
				return false;

			if (i >= c || TbsCert[i++] is not Vector SubjectPublicKeyInfo)
				return false;


			if (SubjectPublicKeyInfo.Length != 2 ||
				SubjectPublicKeyInfo.FirstElement is not IPublicKey PublicKey ||
				!PublicKey.IsConfigured ||
				!PublicKey.SetPublicKey(SubjectPublicKeyInfo.LastElement))
			{
				return false;
			}

			// TODO: Check for optional issuerUniqueID and subjectUniqueID 

			Vector? ListExtensions;
			byte[]? AuthorityKeyIdentifier = null;
			byte[]? SubjectKeyIdentifier = null;

			if (i < c && TbsCert[i] is Vector ListExtensions2)
			{
				i++;

				if (ListExtensions2.Length == 1 &&
					ListExtensions2.FirstElement is Vector ListExtensions3 &&
					(ListExtensions2.SubSection[0] & 0x80) != 0)
				{
					ListExtensions2 = ListExtensions3;
				}

				ListExtensions = ListExtensions2;

				foreach (object? Extension in ListExtensions2)
				{
					if (Extension is AuthorityKeyIdentifier Aki)
						AuthorityKeyIdentifier = Aki.Value;
					else if (Extension is SubjectKeyIdentifier Ski)
						SubjectKeyIdentifier = Ski.Value;
				}
			}
			else
				ListExtensions = null;

			if (i < c)
				return false;

			Parsed = new ToBeSignedCertificate(TbsCert.SubSection, Version, SerialNumber,
				IssuerSignatureAlgorithm, new Names(Issuer), NotBefore, NotAfter,
				new Names(Subject), AuthorityKeyIdentifier, SubjectKeyIdentifier,
				PublicKey, ListExtensions);

			return true;
		}

		/// <summary>
		/// Binary representation of certificate to be signed.
		/// </summary>
		public byte[] Binary { get; }

		/// <summary>
		/// Version of document.
		/// </summary>
		public int Version { get; }

		/// <summary>
		/// Serial Number
		/// </summary>
		public System.Numerics.BigInteger SerialNumber { get; }

		/// <summary>
		/// Signature algorithm used by issuer to sign the certificate.
		/// </summary>
		public ISignatureAlgorithm IssuerSignatureAlgorithm { get; }

		/// <summary>
		/// Issuer
		/// </summary>
		public Names Issuer { get; }

		/// <summary>
		/// Signatures cannot be created before this timestamp.
		/// </summary>
		public DateTimeOffset NotBefore { get; }

		/// <summary>
		/// Signatures cannot be created after this timestamp.
		/// </summary>
		public DateTimeOffset NotAfter { get; }

		/// <summary>
		/// Subject
		/// </summary>
		public Names Subject { get; }

		/// <summary>
		/// Authority Key Identifier
		/// </summary>
		public byte[]? AuthorityKeyIdentifier { get; }

		/// <summary>
		/// Subject Key Identifier
		/// </summary>
		public byte[]? SubjectKeyIdentifier { get; }

		/// <summary>
		/// Certificate public key, used to verify signatures issued by the certificate.
		/// </summary>
		public IPublicKey PublicKey { get; }

		/// <summary>
		/// Extensions
		/// </summary>
		public Vector? Extensions { get; }
	}
}
