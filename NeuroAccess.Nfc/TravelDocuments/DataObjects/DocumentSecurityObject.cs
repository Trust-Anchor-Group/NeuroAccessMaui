using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.Pkcs;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using NeuroAccess.Nfc.TravelDocuments.SignedMessages;
using Waher.Events;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Document Security Object. Reference: §4.6.2, EF.SOD, ICAO Doc 9303-10, Table 36.
	/// </summary>
	public class DocumentSecurityObject : DataObject
	{
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
		/// <param name="SignedData">Signed content.</param>
		/// <param name="LdsSecurityObject">LDS Security Object.</param>
		public DocumentSecurityObject(byte[] Value, SignedData? SignedData, LdsSecurityObject LdsSecurityObject)
			: base(Value)
		{
			this.SignedData = SignedData;
			this.LdsSecurityObject = LdsSecurityObject;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x77;

		/// <summary>
		/// Signed data.
		/// </summary>
		public SignedData? SignedData { get; }

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

			if (Client.AppInfo?.HasAtLeastLdsVersion(1, 8) ?? false)
			{
				try
				{
					if (!SignedMessage.TryParse(Value, out SignedMessage? SignedData))
					{
						Client.Warning("Could not parse Signed CMS:\r\n\r\n " +
							Convert.ToBase64String(Value, Base64FormattingOptions.InsertLineBreaks));
						return false;
					}

					byte[]? Content;
					string? ContentOid;

					// First use platform/OS-independent signature validation implementation.

					if (SignedData.CheckSignature(false, Client))   // No need to validate certificate at this point, as it is validated when the certificate chain is validated.
					{
						Content = SignedData.Data.EncapsulatedContent;
						ContentOid = SignedData.Data.EncapsulatedContentOid;
					}
					else
					{
						if (!Client.PermitPlatformDependentValidation)
							return false;

						// If platform/OS-independent signature validation fails
						// (implemetation error?), double-check with platform/OS-dependent
						// signature validation.

						try
						{
							SignedCms SignedDataX = new();  // Backup, in case of implementation error in SignedMessage.
							SignedDataX.Decode(Value);

							SignedDataX.CheckSignature(true);

							Content = SignedDataX.ContentInfo?.Content;
							ContentOid = SignedDataX.ContentInfo?.ContentType?.Value;

							Log.Debug("Platform/OS-independent signature validation failed, but Platform/OS-dependent signature validation successful. Check communication logs for more details.");

							Client.Warning("Platform/OS-independent signature validation failed, but Platform/OS-dependent signature validation successful:\r\n\r\n" +
								Convert.ToBase64String(Value, Base64FormattingOptions.InsertLineBreaks));
						}
						catch (Exception)
						{
							Client.Warning("Could not validate signatures in Signed CMS:\r\n\r\n " +
								Convert.ToBase64String(Value, Base64FormattingOptions.InsertLineBreaks));
							return false;
						}
					}

					if (Content is null || string.IsNullOrEmpty(ContentOid))
						return false;

					if (!ASN1.TryInstantiate(ContentOid, out ISecurityObject? SecurityObject))
					{
						Client.Warning("OID not recognized: " + ContentOid);
						ASN1.ReportOidNotRecognized(ContentOid);
						return false;
					}

					if (SecurityObject is not LdsSecurityObject LdsSecurityObject)
						return false;

					ASN1.TryDecodeDer(Client, Content, out object? ParsedContent);

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

					Parsed = new DocumentSecurityObject(Value, SignedData.Data, LdsSecurityObject);
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
