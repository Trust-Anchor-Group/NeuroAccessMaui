using System;
using System.Collections.Generic;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using Waher.Networking;
using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.SignedMessages
{
	/// <summary>
	/// Signed Data, as defined in RFC 5652, §5.1.
	/// </summary>
	public class SignedData() : SecurityObject()
	{
		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.7.2";

		/// <summary>
		/// ASN.1 decoded vector
		/// </summary>
		public Vector? Asn1Vector { get; private set; }

		/// <summary>
		/// Version number
		/// </summary>
		public System.Numerics.BigInteger Version { get; private set; }

		/// <summary>
		/// Digest algorithms used to sign the encapsulated content.
		/// </summary>
		public HashFunction[] DigestAlgorithms { get; private set; } = [];

		/// <summary>
		/// Encapsulated content OID.
		/// </summary>
		public string EncapsulatedContentOid { get; private set; } = string.Empty;

		/// <summary>
		/// Encapsulated content.
		/// </summary>
		public byte[] EncapsulatedContent { get; private set; } = [];

		/// <summary>
		/// Signer information
		/// </summary>
		public SignerInfo[] SignerInfos { get; private set; } = [];

		/// <summary>
		/// Certificate set, if included in the message.
		/// </summary>
		public Certificate[] Certificates { get; private set; } = [];

		/// <summary>
		/// Revocation information choices, if included in the message.
		/// </summary>
		public ContextSpecific? RevocationInfoChoices { get; private set; }

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.Asn1Vector is not null;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length != 2 ||
				SecurityInfo.LastElement is not Vector SignedDataVector)
			{
				return false;
			}

			if (SignedDataVector.Length == 1 &&
				SignedDataVector.FirstElement is Vector SignedDataVector2)
			{
				SignedDataVector = SignedDataVector2;
			}

			if (SignedDataVector.Length < 4)
				return false;

			if (SignedDataVector.FirstElement is not System.Numerics.BigInteger Version)
				return false;

			if (SignedDataVector[1] is not Vector DigestAlgorithms)
				return false;

			if (SignedDataVector[2] is not Vector EncapsulatedContent ||
				EncapsulatedContent.Length < 2)
			{
				return false;
			}

			byte[] BinaryContent;
			string ContentOid;

			if (EncapsulatedContent.FirstElement is ISecurityObject SecurityObject)
				ContentOid = SecurityObject.Oid;
			else if (EncapsulatedContent.FirstElement is string s)
				ContentOid = s;
			else if (EncapsulatedContent.FirstElement is Vector v &&
				v.FirstElement is string s2)
			{
				ContentOid = s2;
			}
			else
				return false;

			if (EncapsulatedContent[1] is byte[] Bin)
				BinaryContent = Bin;
			else if (EncapsulatedContent[1] is Vector ContentVector)
			{
				if (ContentVector.Length == 1)
				{
					if (ContentVector.FirstElement is byte[] EmbeddedContent)
						BinaryContent = EmbeddedContent;
					else if (ContentVector.FirstElement is Vector EmbeddedContent2)
						BinaryContent = EmbeddedContent2.SubSection;
					else
						BinaryContent = ContentVector.SubSection;
				}
				else
					BinaryContent = ContentVector.SubSection;
			}
			else
				return false;

			ContextSpecific? CertificateSet = null;
			ContextSpecific? RevocationInfoChoices = null;

			for (int i = 3; i < SignedDataVector.Length - 1; i++)
			{
				if (SignedDataVector[i] is ContextSpecific v)
				{
					switch (v.Tag)
					{
						case 0:
							CertificateSet = v;
							break;

						case 1:
							RevocationInfoChoices = v;
							break;
					}
					break;
				}
			}

			if (SignedDataVector.LastElement is not Vector SignerInfos)
				return false;

			ChunkedList<SignerInfo> SignerInfoList = [];

			foreach (object Item in SignerInfos.Elements)
			{
				if (Item is not Vector SignerInfoVector ||
					!SignerInfo.TryParse(SignerInfoVector, out SignerInfo? Parsed))
				{
					return false;
				}

				SignerInfoList.Add(Parsed);
			}

			ChunkedList<HashFunction> HashAlgorithms = [];

			while (DigestAlgorithms.Length == 1 &&
				DigestAlgorithms.FirstElement is Vector v)
			{
				DigestAlgorithms = v;
			}

			foreach (object Algorithm in DigestAlgorithms.Elements)
			{
				if (Algorithm is HashFunction HashFunction)
					HashAlgorithms.Add(HashFunction);
			}

			ChunkedList<Certificate> Certificates = [];

			if (CertificateSet is not null)
			{
				foreach (object Item in CertificateSet.Elements)
				{
					if (Item is Vector CertificateVector &&
						Certificate.TryParse(CertificateVector, out Certificate? Parsed))
					{
						Certificates.Add(Parsed);
					}
					else
						return false;
				}
			}

			this.Asn1Vector = SecurityInfo;
			this.Version = Version;
			this.DigestAlgorithms = [.. HashAlgorithms];
			this.EncapsulatedContentOid = ContentOid;
			this.EncapsulatedContent = BinaryContent;
			this.Certificates = [.. Certificates];
			this.RevocationInfoChoices = RevocationInfoChoices;
			this.SignerInfos = [.. SignerInfoList];

			return true;
		}

		/// <summary>
		/// Checks if the signature is valid.
		/// </summary>
		/// <param name="VerifyCertificates">If certificates should be verified also.</param>
		/// <returns>If signature, and optional embedded certificates, are valid.</returns>
		public bool CheckSignature(bool VerifyCertificates)
		{
			return this.CheckSignature(VerifyCertificates, null);
		}

		/// <summary>
		/// Checks if the signature is valid.
		/// </summary>
		/// <param name="VerifyCertificates">If certificates should be verified also.</param>
		/// <param name="Client">Optional communication layer, for informative communication logs.</param>
		/// <returns>If signature, and optional embedded certificates, are valid.</returns>
		public bool CheckSignature(bool VerifyCertificates, ICommunicationLayer? Client)
		{
			if (!this.IsConfigured ||
				this.SignerInfos is null ||
				this.EncapsulatedContent is null)
			{
				return false;
			}

			bool SignatureValidated = false;

			foreach (SignerInfo Info in this.SignerInfos)
			{
				if (Info.DigestAlgorithm is null ||
					Info.MessageDigest is null ||
					Info.SignatureAlgorithm is null)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(Info.ContentType) &&
					Info.ContentType != this.EncapsulatedContentOid)
				{
					continue;
				}

				byte[] Digest = Info.DigestAlgorithm.ComputeHash(this.EncapsulatedContent);

				if (Convert.ToBase64String(Digest) != Convert.ToBase64String(Info.MessageDigest))
				{
					Client?.Error("Computed digest does not match message digest in signer info.");
					return false;
				}

				Client?.Information("Computed digest matches message digest in signer info.");

				Certificate? SelectedCertificate = null;

				foreach (Certificate Certificate in this.Certificates)
				{
					if (Info.SerialNumber.HasValue && Certificate.SerialNumber == Info.SerialNumber)
					{
						SelectedCertificate = Certificate;
						break;
					}
					else if (Info.SubjectKeyIdentifier is not null)
					{
						KeyValuePair<string?, byte[]?> P = TravelDocumentsClient.GetSubjectKeyIdentifier(Certificate);

						if (P.Value is not null &&
							Convert.ToBase64String(Info.SubjectKeyIdentifier) ==
							Convert.ToBase64String(P.Value))
						{
							SelectedCertificate = Certificate;
							break;
						}
					}
				}

				if (SelectedCertificate is null)
				{
					Client?.Error("Certificate used to sign message not found among included certificates.");
					continue;
				}

				if (Info.HasSignedAttributes)
					Digest = Info.SignedAttributesData;

				if (Info.SignatureAlgorithm.VerifySignature(Digest, Info.Signature,
					SelectedCertificate.PublicKey, Client))
				{
					Client?.Information("Signature validated successfully.");
					SignatureValidated = true;

					if (VerifyCertificates)
					{
						// TODO: Validate
					}
					break;
				}
				else
				{
					Client?.Error("Signature validation failed.");
					return false;
				}
			}

			if (!SignatureValidated)
			{
				Client?.Error("No signature validated.");
				return false;
			}

			return true;
		}
	}
}
