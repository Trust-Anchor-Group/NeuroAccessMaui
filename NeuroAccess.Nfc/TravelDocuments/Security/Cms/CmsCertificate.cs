using System;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Numerics;
using NeuroAccess.Nfc.TravelDocuments.Certificates;

namespace NeuroAccess.Nfc.TravelDocuments.Security.Cms
{
	/// <summary>
	/// Certificate embedded in a CMS SignedData object.
	/// </summary>
	public class CmsCertificate
	{
		/// <summary>
		/// Creates a CMS certificate wrapper.
		/// </summary>
		/// <param name="RawData">DER encoded certificate.</param>
		/// <param name="Certificate">Parsed certificate.</param>
		/// <param name="Issuer">DER encoded issuer name.</param>
		/// <param name="SerialNumber">Certificate serial number.</param>
		public CmsCertificate(byte[] RawData, Certificate Certificate, byte[] Issuer, BigInteger SerialNumber)
		{
			this.RawData = RawData;
			this.Certificate = Certificate;
			this.Issuer = Issuer;
			this.SerialNumber = SerialNumber;
		}

		/// <summary>
		/// Tries to parse a DER encoded certificate.
		/// </summary>
		/// <param name="RawData">DER encoded certificate.</param>
		/// <param name="Parsed">Parsed CMS certificate.</param>
		/// <returns>If the certificate could be parsed.</returns>
		public static bool TryParse(byte[] RawData, [NotNullWhen(true)] out CmsCertificate? Parsed)
		{
			Parsed = null;

			if (!Certificate.TryParse(RawData, out Certificate? ParsedCertificate))
				return false;

			AsnReader Reader = new(RawData, AsnEncodingRules.DER);
			AsnReader CertificateReader = Reader.ReadSequence();
			byte[] TbsCertificate = CertificateReader.ReadEncodedValue().ToArray();
			AsnReader TbsReader = new(TbsCertificate, AsnEncodingRules.DER);
			AsnReader TbsSequence = TbsReader.ReadSequence();

			if (TbsSequence.HasData &&
				TbsSequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0, true)))
			{
				TbsSequence.ReadEncodedValue();
			}

			if (!TbsSequence.HasData)
				return false;

			BigInteger SerialNumber = TbsSequence.ReadInteger();

			if (!TbsSequence.HasData)
				return false;

			TbsSequence.ReadEncodedValue();   // signature

			if (!TbsSequence.HasData)
				return false;

			byte[] Issuer = TbsSequence.ReadEncodedValue().ToArray();

			Parsed = new CmsCertificate(RawData, ParsedCertificate, Issuer, SerialNumber);
			return true;
		}

		/// <summary>
		/// DER encoded certificate.
		/// </summary>
		public byte[] RawData { get; }

		/// <summary>
		/// Parsed certificate.
		/// </summary>
		public Certificate Certificate { get; }

		/// <summary>
		/// DER encoded issuer name.
		/// </summary>
		public byte[] Issuer { get; }

		/// <summary>
		/// Certificate serial number.
		/// </summary>
		public BigInteger SerialNumber { get; }
	}
}
