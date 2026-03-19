using System;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using Waher.Runtime.Collections;
using Waher.Runtime.IO;

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
			/* §3.11.4, ICAO 9303-10, EF.CardSecurity
			 * 
			 *	SignedData ::= SEQUENCE{  
			 *		version CMSVersion,  
			 *		digestAlgorithms DigestAlgorithmIdentifiers, 
			 *		encapContentInfo EncapsulatedContentInfo,  
			 *		certificates [0] IMPLICIT CertificateSet OPTIONAL,  
			 *		crls [1] IMPLICIT RevocationInfoChoices OPTIONAL, 
			 *		signerInfos SignerInfos  
			 *	}			 
			 */

			if (SecurityInfo is null ||
				SecurityInfo.Length < 2 ||
				SecurityInfo.GetValue(1) is not ContextSpecific Properties ||
				Properties.Elements.Length < 1 ||
				Properties.Elements.GetValue(0) is not Vector Properties2 ||
				Properties2.Elements.Length < 3 ||
				Properties2.Elements.GetValue(0) is not BigInteger Version ||
				Properties2.Elements.GetValue(1) is not Vector DigestAlgorithms ||
				Properties2.Elements.GetValue(2) is not LdsSecurityObject EncapsulatedContentInformation)
			{
				return false;
			}

			this.Version = (int)Version;
			this.DigestAlgorithms = DigestAlgorithms.Elements;
			this.EncapsulatedContentInformation = EncapsulatedContentInformation;

			if (Properties2.Elements.Length >= 3)
			{
				if (Properties2.Elements.GetValue(3) is not Vector Certificates)
					return false;

				// Ref: CertificateSet in RFC 5652.

				ChunkedList<X509Certificate2> Certificates2 = [];

				foreach (object Element in Certificates.Elements)
				{
					if (Element is Vector ElementVector)
					{
						X509Certificate2 Certificate = new(ElementVector.SubSection);
						Certificates2.Add(Certificate);
					}
				}

				this.Certificates = [.. Certificates2];
			}
			else
				this.Certificates = null;

			if (Properties2.Elements.Length >= 4)
			{
				if (Properties2.Elements.GetValue(4) is not Vector SignerInformations)
					return false;

				this.SignerInformations = SignerInformations.Elements;
			}
			else
				this.SignerInformations = null;

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
		public X509Certificate2[]? Certificates { get; private set; }

		/// <summary>
		/// Signer Informations
		/// </summary>
		public Array? SignerInformations { get; private set; }
	}
}
