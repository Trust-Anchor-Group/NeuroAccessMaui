using System;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Numerics;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Cms
{
	/// <summary>
	/// SignerInfo entry in a CMS SignedData object.
	/// </summary>
	public class CmsSignerInfo
	{
		private const string ContentTypeAttributeOid = "1.2.840.113549.1.9.3";
		private const string MessageDigestAttributeOid = "1.2.840.113549.1.9.4";

		/// <summary>
		/// Creates a CMS signer information record.
		/// </summary>
		/// <param name="Issuer">DER encoded issuer name, if issuer-and-serial-number identification is used.</param>
		/// <param name="SerialNumber">Certificate serial number, if issuer-and-serial-number identification is used.</param>
		/// <param name="DigestAlgorithm">DER encoded digest algorithm identifier.</param>
		/// <param name="SignedAttributes">DER encoded signed attributes using a universal SET tag.</param>
		/// <param name="ContentType">Signed content type attribute.</param>
		/// <param name="MessageDigest">Signed message digest attribute.</param>
		/// <param name="SignatureAlgorithm">DER encoded signature algorithm identifier.</param>
		/// <param name="Signature">Signature value.</param>
		public CmsSignerInfo(byte[]? Issuer, BigInteger? SerialNumber, byte[] DigestAlgorithm,
			byte[]? SignedAttributes, string? ContentType, byte[]? MessageDigest,
			byte[] SignatureAlgorithm, byte[] Signature)
		{
			this.Issuer = Issuer;
			this.SerialNumber = SerialNumber;
			this.DigestAlgorithm = DigestAlgorithm;
			this.SignedAttributes = SignedAttributes;
			this.ContentType = ContentType;
			this.MessageDigest = MessageDigest;
			this.SignatureAlgorithm = SignatureAlgorithm;
			this.Signature = Signature;
		}

		/// <summary>
		/// Tries to parse a DER encoded SignerInfo structure.
		/// </summary>
		/// <param name="RawData">DER encoded SignerInfo structure.</param>
		/// <param name="Parsed">Parsed SignerInfo.</param>
		/// <returns>If the SignerInfo could be parsed.</returns>
		public static bool TryParse(byte[] RawData, [NotNullWhen(true)] out CmsSignerInfo? Parsed)
		{
			Parsed = null;

			AsnReader Reader = new(RawData, AsnEncodingRules.DER, CmsSignedData.ReaderOptions);
			AsnReader SignerInfo = Reader.ReadSequence();

			SignerInfo.ReadInteger(); // version

			byte[]? Issuer = null;
			BigInteger? SerialNumber = null;

			Asn1Tag SignerIdentifierTag = SignerInfo.PeekTag();
			if (SignerIdentifierTag.TagClass == TagClass.Universal &&
				SignerIdentifierTag.TagValue == (int)UniversalTagNumber.Sequence)
			{
				byte[] SignerIdentifier = SignerInfo.ReadEncodedValue().ToArray();
				AsnReader SignerIdentifierReader = new(SignerIdentifier, AsnEncodingRules.DER, CmsSignedData.ReaderOptions);
				AsnReader IssuerAndSerial = SignerIdentifierReader.ReadSequence();

				Issuer = IssuerAndSerial.ReadEncodedValue().ToArray();
				SerialNumber = IssuerAndSerial.ReadInteger();

				if (IssuerAndSerial.HasData || SignerIdentifierReader.HasData)
					return false;
			}
			else
				return false;

			byte[] DigestAlgorithm = SignerInfo.ReadEncodedValue().ToArray();
			byte[]? SignedAttributes = null;
			string? ContentType = null;
			byte[]? MessageDigest = null;

			if (SignerInfo.HasData &&
				SignerInfo.PeekTag().HasSameClassAndValue(CmsSignedData.ContextSpecific0))
			{
				byte[] ContextSpecificSignedAttributes = SignerInfo.ReadEncodedValue().ToArray();

				if (ContextSpecificSignedAttributes.Length == 0)
					return false;

				SignedAttributes = (byte[])ContextSpecificSignedAttributes.Clone();
				SignedAttributes[0] = 0x31;

				if (!TryParseSignedAttributes(SignedAttributes, out ContentType, out MessageDigest))
					return false;
			}

			byte[] SignatureAlgorithm = SignerInfo.ReadEncodedValue().ToArray();
			byte[] Signature = SignerInfo.ReadOctetString();

			if (SignerInfo.HasData &&
				SignerInfo.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1, true)))
			{
				SignerInfo.ReadEncodedValue();
			}

			if (SignerInfo.HasData || Reader.HasData)
				return false;

			Parsed = new CmsSignerInfo(Issuer, SerialNumber, DigestAlgorithm, SignedAttributes,
				ContentType, MessageDigest, SignatureAlgorithm, Signature);
			return true;
		}

		/// <summary>
		/// Verifies this SignerInfo against the signed content and embedded certificate.
		/// </summary>
		/// <param name="ExpectedContentType">Expected encapsulated content type.</param>
		/// <param name="EncapsulatedContent">Encapsulated content bytes.</param>
		/// <param name="Certificate">Certificate used to verify the signature.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the signature is valid.</returns>
		public bool VerifySignature(string ExpectedContentType, byte[] EncapsulatedContent,
			CmsCertificate Certificate, ICommunicationLayer? Client)
		{
			HashFunction? DigestAlgorithm = this.DecodeDigestAlgorithm(Client);
			if (DigestAlgorithm is null)
				return false;

			byte[] DataToVerify;

			if (this.SignedAttributes is not null)
			{
				if (!string.Equals(this.ContentType, ExpectedContentType, StringComparison.Ordinal))
				{
					Client?.Error("CMS signed content type attribute does not match EF.SOD content type.");
					return false;
				}

				if (this.MessageDigest is null)
				{
					Client?.Error("CMS signed messageDigest attribute missing.");
					return false;
				}

				byte[] ExpectedDigest = DigestAlgorithm.ComputeHash(EncapsulatedContent);
				if (!AreEqual(ExpectedDigest, this.MessageDigest))
				{
					Client?.Error("CMS signed messageDigest attribute does not match EF.SOD content.");
					return false;
				}

				DataToVerify = this.SignedAttributes;
			}
			else
				DataToVerify = EncapsulatedContent;

			ISignatureAlgorithm? SignatureAlgorithm = this.DecodeSignatureAlgorithm(Client);
			if (SignatureAlgorithm is null)
				return false;

			return SignatureAlgorithm.VerifySignature(DataToVerify, this.Signature,
				Certificate.Certificate.PublicKey, Client);
		}

		private static bool TryParseSignedAttributes(byte[] SignedAttributes,
			out string? ContentType, out byte[]? MessageDigest)
		{
			ContentType = null;
			MessageDigest = null;

			AsnReader Reader = new(SignedAttributes, AsnEncodingRules.DER, CmsSignedData.ReaderOptions);
			AsnReader Attributes = Reader.ReadSetOf();

			while (Attributes.HasData)
			{
				AsnReader Attribute = Attributes.ReadSequence();
				string AttributeOid = Attribute.ReadObjectIdentifier();
				AsnReader Values = Attribute.ReadSetOf();

				if (string.Equals(AttributeOid, ContentTypeAttributeOid, StringComparison.Ordinal))
					ContentType = Values.ReadObjectIdentifier();
				else if (string.Equals(AttributeOid, MessageDigestAttributeOid, StringComparison.Ordinal))
					MessageDigest = Values.ReadOctetString();
				else
					Values.ReadEncodedValue();

				if (Values.HasData || Attribute.HasData)
					return false;
			}

			return !Reader.HasData;
		}

		private HashFunction? DecodeDigestAlgorithm(ICommunicationLayer? Client)
		{
			if (!ASN1.TryDecodeDer(Client, this.DigestAlgorithm, out object? Decoded))
			{
				Client?.Error("Unable to decode CMS digest algorithm.");
				return null;
			}

			if (Decoded is HashFunction DecodedHashFunction)
				return DecodedHashFunction;

			if (Decoded is not Vector AlgorithmIdentifier || AlgorithmIdentifier.Length == 0)
			{
				Client?.Error("CMS digest algorithm not supported.");
				return null;
			}

			if (AlgorithmIdentifier[0] is HashFunction HashFunction)
				return HashFunction;

			Client?.Error("CMS digest algorithm not supported.");
			return null;
		}

		private ISignatureAlgorithm? DecodeSignatureAlgorithm(ICommunicationLayer? Client)
		{
			if (!ASN1.TryDecodeDer(Client, this.SignatureAlgorithm, out object? Decoded))
			{
				Client?.Error("Unable to decode CMS signature algorithm.");
				return null;
			}

			if (Decoded is ISignatureAlgorithm SignatureAlgorithm)
				return SignatureAlgorithm;

			if (Decoded is not Vector AlgorithmIdentifier)
			{
				Client?.Error("CMS signature algorithm not supported.");
				return null;
			}

			return NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms.SignatureAlgorithm.TryDecode(
				AlgorithmIdentifier, Client);
		}

		private static bool AreEqual(byte[] A, byte[] B)
		{
			int c = A.Length;
			if (B.Length != c)
				return false;

			int Difference = 0;
			for (int i = 0; i < c; i++)
				Difference |= A[i] ^ B[i];

			return Difference == 0;
		}

		/// <summary>
		/// DER encoded issuer name, if issuer-and-serial-number identification is used.
		/// </summary>
		public byte[]? Issuer { get; }

		/// <summary>
		/// Certificate serial number, if issuer-and-serial-number identification is used.
		/// </summary>
		public BigInteger? SerialNumber { get; }

		/// <summary>
		/// DER encoded digest algorithm identifier.
		/// </summary>
		public byte[] DigestAlgorithm { get; }

		/// <summary>
		/// DER encoded signed attributes using a universal SET tag.
		/// </summary>
		public byte[]? SignedAttributes { get; }

		/// <summary>
		/// Signed content type attribute.
		/// </summary>
		public string? ContentType { get; }

		/// <summary>
		/// Signed message digest attribute.
		/// </summary>
		public byte[]? MessageDigest { get; }

		/// <summary>
		/// DER encoded signature algorithm identifier.
		/// </summary>
		public byte[] SignatureAlgorithm { get; }

		/// <summary>
		/// Signature value.
		/// </summary>
		public byte[] Signature { get; }
	}
}
