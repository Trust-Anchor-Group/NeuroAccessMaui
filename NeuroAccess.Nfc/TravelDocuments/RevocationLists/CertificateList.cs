using NeuroAccess.Nfc.TravelDocuments.Security;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;

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
		/// <param name="ToBeSignedCertificateList">List of certificates that is signed.</param>
		/// <param name="SignatureAlgorithm">Algorithm used to sign the certificate list.</param>
		/// <param name="Signature">Digital signature.</param>
		private CertificateList(ToBeSignedCertificateList ToBeSignedCertificateList,
			Vector SignatureAlgorithm, byte[] Signature)
		{
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

			if (Crl.Elements.GetValue(2) is not byte[] Signature)
				return false;

			if (!ToBeSignedCertificateList.TryParse(TbsCertList, out ToBeSignedCertificateList? ToBeSigned))
				return false;

			Parsed = new CertificateList(ToBeSigned, AlgorithmIdentifier, Signature);

			return true;
		}

		/// <summary>
		/// List of certificates that is signed.
		/// </summary>
		public ToBeSignedCertificateList ToBeSignedCertificateList { get; }

		/// <summary>
		/// Algorithm used to sign the certificate list.
		/// </summary>
		public Vector SignatureAlgorithm { get; }

		/// <summary>
		/// Digital signature.
		/// </summary>
		public byte[] Signature { get; }
	}
}
