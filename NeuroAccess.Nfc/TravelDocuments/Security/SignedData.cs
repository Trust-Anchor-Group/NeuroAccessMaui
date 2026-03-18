using System;
using System.Numerics;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Contents of EF.SOD. Reference: §4.6.2, ICAO Doc 9303-10.
	/// </summary>
	public class SignedData : SecurityObject
	{
		/// <summary>
		/// Contents of EF.SOD. Reference: §4.6.2, ICAO Doc 9303-10.
		/// </summary>
		public SignedData()
		{
		}

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.7.2";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Array SecurityInfo)
		{
			if (SecurityInfo is null ||
				SecurityInfo.Length < 2 ||
				SecurityInfo.GetValue(1) is not Vector Properties ||
				Properties.Elements.Length < 5 ||
				Properties.Elements.GetValue(0) is not BigInteger Version ||
				Properties.Elements.GetValue(1) is not Vector DigestAlgorithms ||
				Properties.Elements.GetValue(2) is not LdsSecurityObject EncapsulatedContentInformation ||
				Properties.Elements.GetValue(3) is not Vector Certificates ||
				Properties.Elements.GetValue(4) is not Vector SignerInformations)
			{
				return false;
			}

			this.Version = (int)Version;
			this.DigestAlgorithms = DigestAlgorithms.Elements;
			this.EncapsulatedContentInformation = EncapsulatedContentInformation;
			this.Certificates = Certificates.Elements;
			this.SignerInformations = SignerInformations.Elements;

			return true;
		}

		/// <summary>
		/// Version of document.
		/// </summary>
		public int Version { get; private set; }

		/// <summary>
		/// Digest algorithms
		/// </summary>
		public Array? DigestAlgorithms { get; private set; }

		/// <summary>
		/// Encapsulated content information
		/// </summary>
		public LdsSecurityObject? EncapsulatedContentInformation { get; private set; }

		/// <summary>
		/// Certificates
		/// </summary>
		public Array? Certificates { get; private set; }

		/// <summary>
		/// Signer Informations
		/// </summary>
		public Array? SignerInformations { get; private set; }
	}
}
