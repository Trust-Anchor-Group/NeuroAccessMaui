using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using NeuroAccess.Nfc.TravelDocuments.Security.Properties.DistinguishedNames;
using NeuroAccess.Nfc.TravelDocuments.Security.Properties.Keys;
using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.SignedMessages
{
	/// <summary>
	/// Signer Information, as defined in RFC 5652, §5.3.
	/// </summary>
	public class SignerInfo
	{
		/// <summary>
		/// Signer Information, as defined in RFC 5652, §5.3.
		/// </summary>
		private SignerInfo()
		{
		}

		/// <summary>
		/// Version of encoding
		/// </summary>
		public System.Numerics.BigInteger Version { get; private set; }

		/// <summary>
		/// Information about signed.
		/// </summary>
		public Names? Signer { get; private set; }

		/// <summary>
		/// Signer serial number.
		/// </summary>
		public System.Numerics.BigInteger? SerialNumber { get; private set; }

		/// <summary>
		/// Signer Subject Key Identifier.
		/// </summary>
		public byte[]? SubjectKeyIdentifier { get; private set; }

		/// <summary>
		/// Digest algorithm.
		/// </summary>
		public HashFunction? DigestAlgorithm { get; private set; }

		/// <summary>
		/// Signed attributes
		/// </summary>
		public object[] SignedAttributes { get; private set; } = [];

		/// <summary>
		/// Binart representation of the signed attributes, as they are used in signature
		/// validation.
		/// </summary>
		public byte[] SignedAttributesData { get; private set; } = [];

		/// <summary>
		/// If there are signed attributes.
		/// </summary>
		public bool HasSignedAttributes { get; private set; } = false;

		/// <summary>
		/// OID of content
		/// </summary>
		public string? ContentType { get; private set; }

		/// <summary>
		/// Message digest
		/// </summary>
		public byte[]? MessageDigest { get; private set; }

		/// <summary>
		/// Signature algorithm
		/// </summary>
		public ISignatureAlgorithm? SignatureAlgorithm { get; private set; }

		/// <summary>
		/// Signature
		/// </summary>
		public byte[] Signature { get; private set; } = [];

		/// <summary>
		/// Unsigned attributes
		/// </summary>
		public object[] UnsignedAttributes { get; private set; } = [];

		/// <summary>
		/// Tries to parse an ASN.1-encoded Signer Information structure, as defined in
		/// RFC 5652, §5.3.
		/// </summary>
		/// <param name="SignerInfoVector">Decoded Signer Information object.</param>
		/// <param name="Parsed">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(Vector SignerInfoVector, [NotNullWhen(true)] out SignerInfo? Parsed)
		{
			Parsed = null;

			int i, c;

			if ((c = SignerInfoVector.Length) < 5)
				return false;

			if (SignerInfoVector.FirstElement is not System.Numerics.BigInteger Version)
				return false;

			if (SignerInfoVector[1] is not Vector SignerIdentifierVector)
				return false;

			System.Numerics.BigInteger? SerialNumber = null;
			byte[]? SubjectKeyIdentifier = null;
			Names? Signer = null;

			foreach (object Item in SignerIdentifierVector.Elements)
			{
				if (Item is Vector v)
					Signer ??= new Names(v);
				else if (Item is System.Numerics.BigInteger SN)
					SerialNumber = SN;
				else if (Item is SubjectKeyIdentifier Ski)
					SubjectKeyIdentifier = Ski.Identifier;
				else if (Item is byte[] Bin)
					SubjectKeyIdentifier = Bin;
			}

			if (SignerInfoVector[2] is not Vector DigestAlgorithmVector ||
				DigestAlgorithmVector.FirstElement is not HashFunction DigestAlgorithm)
			{
				return false;
			}

			i = 3;

			ChunkedList<object> UnsignedAttributes = [];
			ChunkedList<object> SignedAttributes = [];
			byte[] SignedAttributesBinary = [];
			ContentType? ContentType = null;
			MessageDigest? MessageDigest = null;
			bool HasSignedAttributes = false;

			if (i < c &&
				SignerInfoVector[i] is ContextSpecific SignedAttributesVector &&
				SignedAttributesVector.Tag == 0)
			{
				SignedAttributesBinary = (byte[])SignedAttributesVector.SubSection.Clone();
				SignedAttributesBinary[0] = (byte)UniversalTagNumber.Set | 0x20;
				HasSignedAttributes = true;

				foreach (object Attribute in SignedAttributesVector.Elements)
				{
					if (Attribute is ContentType ContentTypeAttribute)
						ContentType = ContentTypeAttribute;
					else if (Attribute is MessageDigest MessageDigestAttribute)
						MessageDigest = MessageDigestAttribute;

					SignedAttributes.Add(Attribute);
				}

				if (ContentType is null || MessageDigest is null)
					return false;

				i++;
			}

			if (i >= c)
				return false;

			ISignatureAlgorithm? SignatureAlgorithm = SignerInfoVector[i] as ISignatureAlgorithm;

			if (SignatureAlgorithm is null)
			{
				if (SignerInfoVector[i] is Vector AlgorithmIdentifier)
				{
					SignatureAlgorithm = Security.SignatureAlgorithms.SignatureAlgorithm.TryDecode(AlgorithmIdentifier);
					if (SignatureAlgorithm is null)
						return false;
				}
				else
					return false;
			}

			i++;

			if (i >= c)
				return false;

			if (SignerInfoVector[i] is not byte[] Signature)
			{
				if (SignerInfoVector[i] is Vector v)
					Signature = v.SubSection;
				else
					return false;
			}

			i++;

			if (i < c &&
				SignerInfoVector[i] is ContextSpecific UnsignedAttributesVector &&
				UnsignedAttributesVector.Tag == 1)
			{
				foreach (object Attribute in UnsignedAttributesVector.Elements)
					UnsignedAttributes.Add(Attribute);

				i++;
			}

			Parsed = new SignerInfo()
			{
				Version = Version,
				Signer = Signer,
				SerialNumber = SerialNumber,
				SubjectKeyIdentifier = SubjectKeyIdentifier,
				DigestAlgorithm = DigestAlgorithm,
				SignedAttributes = [.. SignedAttributes],
				SignedAttributesData = SignedAttributesBinary,
				HasSignedAttributes = HasSignedAttributes,
				ContentType = ContentType?.Value,
				MessageDigest = MessageDigest?.Value,
				SignatureAlgorithm = SignatureAlgorithm,
				Signature = Signature,
				UnsignedAttributes = [.. UnsignedAttributes]
			};

			return true;
		}
	}
}
