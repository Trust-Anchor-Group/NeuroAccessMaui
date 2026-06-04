using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.Pkcs;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.Cms;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Document Security Object. Reference: §4.6.2, EF.SOD, ICAO Doc 9303-10, Table 36.
	/// </summary>
	public class DocumentSecurityObject : DataObject
	{
		private const string LdsSecurityObjectOid = "2.23.136.1.1.1";

		/// <summary>
		/// Document Security Object. Reference: §4.6.2, EF.SOD, ICAO Doc 9303-10, Table 36.
		/// </summary>
		public DocumentSecurityObject()
			: base([])
		{
		}

		/// <summary>
		/// Document Security Object. Reference: §4.6.2, EF.SOD, ICAO Doc 9303-10, Table 36.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="SignedData">Parsed content.</param>
		/// <param name="CmsSignedData">Parsed CMS signed data.</param>
		/// <param name="LdsSecurityObject">LDS Security Object.</param>
		public DocumentSecurityObject(byte[] Value, SignedCms? SignedData, CmsSignedData CmsSignedData,
			LdsSecurityObject LdsSecurityObject)
			: base(Value)
		{
			this.SignedData = SignedData;
			this.CmsSignedData = CmsSignedData;
			this.LdsSecurityObject = LdsSecurityObject;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x77;

		/// <summary>
		/// Signed data.
		/// </summary>
		public SignedCms? SignedData { get; }

		/// <summary>
		/// CMS signed data parsed with the repo-owned verifier.
		/// </summary>
		public CmsSignedData? CmsSignedData { get; }

		/// <summary>
		/// Certificates embedded in the CMS signed data.
		/// </summary>
		public Certificate[] Certificates => this.CmsSignedData?.Certificates ?? [];

		/// <summary>
		/// Number of signer information entries.
		/// </summary>
		public int SignerCount => this.CmsSignedData?.SignerInfos.Length ?? 0;

		/// <summary>
		/// Number of embedded certificates.
		/// </summary>
		public int CertificateCount => this.Certificates.Length;

		/// <summary>
		/// If the EF.SOD CMS signature has been verified.
		/// </summary>
		public bool SignatureVerified => this.CmsSignedData?.SignatureVerified ?? false;

		/// <summary>
		/// >LDS Security Object.
		/// </summary>
		public LdsSecurityObject? LdsSecurityObject { get; }

		/// <summary>
		/// Tries to parse a binary representation of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <param name="Parsed">Parsed object, if successful.</param>
		/// <returns>If value could be parsed.</returns>
		public override bool TryParse(byte[] Value, TravelDocumentsClient Client,
			[NotNullWhen(true)] out IDataObject? Parsed)
		{
			Parsed = null;

			if (Client.AppInfo?.IsLdsVersionAtLeast(1, 8) ?? false)
			{
				try
				{
					SignedCms? SignedData = TryDecodeSignedCms(Value);

					if (!CmsSignedData.TryParse(Value, LdsSecurityObjectOid, Client,
						out CmsSignedData? ParsedCmsSignedData))
					{
						return false;
					}

					if (!ASN1.TryInstantiate(ParsedCmsSignedData.ContentType, out ISecurityObject? SecurityObject))
					{
						Client.Warning("OID not recognized: " + ParsedCmsSignedData.ContentType);
						ASN1.ReportOidNotRecognized(ParsedCmsSignedData.ContentType);
						return false;
					}

					if (SecurityObject is not LdsSecurityObject LdsSecurityObject)
						return false;

					ASN1.TryDecodeDer(Client, ParsedCmsSignedData.EncapsulatedContent, out object? ParsedContent);

					if (ParsedContent is not Vector ContentVector)
						return false;

					if (!LdsSecurityObject.IsConfigured)
					{
						if (!LdsSecurityObject.Configure(ContentVector))
						{
							ASN1.ReportOidNotConfigured(LdsSecurityObject.Oid);
							return false;
						}
					}

					Parsed = new DocumentSecurityObject(Value, SignedData, ParsedCmsSignedData, LdsSecurityObject);
					return true;
				}
				catch (Exception ex)
				{
					Client.Exception(ex);
					return false;
				}
			}
			else
			{
				// TODO: LDS version < 1.8 support

				return false;
			}
		}

		private static SignedCms? TryDecodeSignedCms(byte[] Value)
		{
			try
			{
				SignedCms SignedData = new();
				SignedData.Decode(Value);
				return SignedData;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Validates data read from a data group, using the information in the LDS Security Object.
		/// </summary>
		/// <param name="Nr">Data group number.</param>
		/// <param name="DataRead">Data read.</param>
		/// <returns>If the data is valid in accordance with the signatures available.</returns>
		public bool ValidateDataGroup(int Nr, byte[] DataRead)
		{
			if (!(this.LdsSecurityObject?.DataGroupHashValues?.TryGetValue(Nr, out byte[]? ExpectedDigest) ?? false))
				return false;

			HashFunction[]? HashFunctions = this.LdsSecurityObject!.HashFunctions;
			if (HashFunctions is null)
				return false;

			int i, c = ExpectedDigest.Length;

			foreach (HashFunction H in HashFunctions)
			{
				byte[] Digest = H.ComputeHash(DataRead);

				if (Digest.Length != c)
					continue;

				for (i = 0; i < c; i++)
				{
					if (Digest[i] != ExpectedDigest[i])
						break;
				}

				if (i == c)
					return true;
			}

			return false;
		}
	}
}
