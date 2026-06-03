using System;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Linq;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using Waher.Networking;
using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Cms
{
	/// <summary>
	/// CMS SignedData object used by EF.SOD.
	/// </summary>
	public class CmsSignedData
	{
		/// <summary>
		/// CMS signedData content type OID.
		/// </summary>
		public const string SignedDataContentTypeOid = "1.2.840.113549.1.7.2";

		/// <summary>
		/// Creates a CMS SignedData object.
		/// </summary>
		/// <param name="ContentType">Encapsulated content type OID.</param>
		/// <param name="EncapsulatedContent">Encapsulated content.</param>
		/// <param name="CertificateEntries">Embedded certificates.</param>
		/// <param name="SignerInfos">Signer information entries.</param>
		/// <param name="SignatureVerified">If all signer signatures were verified.</param>
		public CmsSignedData(string ContentType, byte[] EncapsulatedContent,
			CmsCertificate[] CertificateEntries, CmsSignerInfo[] SignerInfos, bool SignatureVerified)
		{
			this.ContentType = ContentType;
			this.EncapsulatedContent = EncapsulatedContent;
			this.CertificateEntries = CertificateEntries;
			this.SignerInfos = SignerInfos;
			this.SignatureVerified = SignatureVerified;
			this.Certificates = CertificateEntries.Select(static Entry => Entry.Certificate).ToArray();
		}

		/// <summary>
		/// Tries to parse a CMS SignedData ContentInfo object.
		/// </summary>
		/// <param name="RawData">DER encoded CMS ContentInfo.</param>
		/// <param name="ExpectedContentType">Expected encapsulated content type OID.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <param name="Parsed">Parsed CMS SignedData object.</param>
		/// <returns>If the object could be parsed and verified.</returns>
		public static bool TryParse(byte[] RawData, string ExpectedContentType, ICommunicationLayer? Client,
			[NotNullWhen(true)] out CmsSignedData? Parsed)
		{
			Parsed = null;

			try
			{
				if (!TryParseRaw(RawData, Client, out CmsSignedData? SignedData))
					return false;

				if (!string.Equals(SignedData.ContentType, ExpectedContentType, StringComparison.Ordinal))
				{
					Client?.Error("CMS encapsulated content type is not the expected EF.SOD content type.");
					return false;
				}

				if (!SignedData.VerifySignatures(ExpectedContentType, Client))
					return false;

				Parsed = new CmsSignedData(SignedData.ContentType, SignedData.EncapsulatedContent,
					SignedData.CertificateEntries, SignedData.SignerInfos, true);
				return true;
			}
			catch (Exception ex)
			{
				Client?.Error("Unable to parse CMS SignedData: " + ex.Message);
				return false;
			}
		}

		internal static readonly AsnReaderOptions ReaderOptions = new()
		{
			SkipSetSortOrderVerification = true
		};

		internal static readonly Asn1Tag ContextSpecific0 = new(TagClass.ContextSpecific, 0, true);

		private static bool TryParseRaw(byte[] RawData, ICommunicationLayer? Client,
			[NotNullWhen(true)] out CmsSignedData? Parsed)
		{
			Parsed = null;

			AsnReader Reader = new(RawData, AsnEncodingRules.DER, ReaderOptions);
			AsnReader ContentInfo = Reader.ReadSequence();
			string ContentType = ContentInfo.ReadObjectIdentifier();

			if (!string.Equals(ContentType, SignedDataContentTypeOid, StringComparison.Ordinal))
			{
				Client?.Error("CMS ContentInfo does not contain signedData.");
				return false;
			}

			AsnReader SignedDataExplicit = ContentInfo.ReadSequence(ContextSpecific0);
			AsnReader SignedData = SignedDataExplicit.ReadSequence();

			SignedData.ReadInteger(); // version

			AsnReader DigestAlgorithms = SignedData.ReadSetOf();
			while (DigestAlgorithms.HasData)
				DigestAlgorithms.ReadEncodedValue();

			AsnReader EncapsulatedContentInfo = SignedData.ReadSequence();
			string EncapsulatedContentType = EncapsulatedContentInfo.ReadObjectIdentifier();

			if (!EncapsulatedContentInfo.HasData ||
				!EncapsulatedContentInfo.PeekTag().HasSameClassAndValue(ContextSpecific0))
			{
				Client?.Error("CMS SignedData does not contain encapsulated content.");
				return false;
			}

			AsnReader EncapsulatedContentExplicit = EncapsulatedContentInfo.ReadSequence(ContextSpecific0);
			byte[] EncapsulatedContent = EncapsulatedContentExplicit.ReadOctetString();

			if (EncapsulatedContentExplicit.HasData || EncapsulatedContentInfo.HasData)
				return false;

			ChunkedList<CmsCertificate> CertificateEntries = [];

			if (SignedData.HasData &&
				SignedData.PeekTag().HasSameClassAndValue(ContextSpecific0))
			{
				AsnReader Certificates = SignedData.ReadSetOf(ContextSpecific0);

				while (Certificates.HasData)
				{
					Asn1Tag CertificateTag = Certificates.PeekTag();
					byte[] RawCertificate = Certificates.ReadEncodedValue().ToArray();

					if (CertificateTag.TagClass != TagClass.Universal ||
						CertificateTag.TagValue != (int)UniversalTagNumber.Sequence)
					{
						continue;
					}

					if (!CmsCertificate.TryParse(RawCertificate, out CmsCertificate? Certificate))
					{
						Client?.Error("Unable to parse CMS certificate.");
						return false;
					}

					CertificateEntries.Add(Certificate);
				}
			}

			if (SignedData.HasData &&
				SignedData.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1, true)))
			{
				SignedData.ReadEncodedValue();
			}

			AsnReader SignerInfosReader = SignedData.ReadSetOf();
			ChunkedList<CmsSignerInfo> SignerInfos = [];

			while (SignerInfosReader.HasData)
			{
				byte[] RawSignerInfo = SignerInfosReader.ReadEncodedValue().ToArray();
				if (!CmsSignerInfo.TryParse(RawSignerInfo, out CmsSignerInfo? SignerInfo))
				{
					Client?.Error("Unable to parse CMS SignerInfo.");
					return false;
				}

				SignerInfos.Add(SignerInfo);
			}

			if (SignedData.HasData || SignedDataExplicit.HasData || ContentInfo.HasData || Reader.HasData)
				return false;

			Parsed = new CmsSignedData(EncapsulatedContentType, EncapsulatedContent,
				[.. CertificateEntries], [.. SignerInfos], false);
			return true;
		}

		private bool VerifySignatures(string ExpectedContentType, ICommunicationLayer? Client)
		{
			if (this.SignerInfos.Length == 0)
			{
				Client?.Error("CMS SignedData does not contain SignerInfo entries.");
				return false;
			}

			foreach (CmsSignerInfo SignerInfo in this.SignerInfos)
			{
				CmsCertificate? Certificate = this.FindCertificate(SignerInfo);
				if (Certificate is null)
				{
					Client?.Error("Unable to match CMS SignerInfo to an embedded certificate.");
					return false;
				}

				if (!SignerInfo.VerifySignature(ExpectedContentType, this.EncapsulatedContent,
					Certificate, Client))
				{
					Client?.Error("CMS SignerInfo signature is not valid.");
					return false;
				}
			}

			return true;
		}

		private CmsCertificate? FindCertificate(CmsSignerInfo SignerInfo)
		{
			if (SignerInfo.Issuer is null || !SignerInfo.SerialNumber.HasValue)
				return null;

			foreach (CmsCertificate Certificate in this.CertificateEntries)
			{
				if (Certificate.SerialNumber == SignerInfo.SerialNumber.Value &&
					AreEqual(Certificate.Issuer, SignerInfo.Issuer))
				{
					return Certificate;
				}
			}

			return null;
		}

		private static bool AreEqual(byte[] A, byte[] B)
		{
			int c = A.Length;
			if (B.Length != c)
				return false;

			for (int i = 0; i < c; i++)
			{
				if (A[i] != B[i])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Encapsulated content type OID.
		/// </summary>
		public string ContentType { get; }

		/// <summary>
		/// Encapsulated content.
		/// </summary>
		public byte[] EncapsulatedContent { get; }

		/// <summary>
		/// Embedded certificate entries.
		/// </summary>
		public CmsCertificate[] CertificateEntries { get; }

		/// <summary>
		/// Parsed embedded certificates.
		/// </summary>
		public Certificate[] Certificates { get; }

		/// <summary>
		/// Signer information entries.
		/// </summary>
		public CmsSignerInfo[] SignerInfos { get; }

		/// <summary>
		/// If all signer signatures were verified.
		/// </summary>
		public bool SignatureVerified { get; }
	}
}
