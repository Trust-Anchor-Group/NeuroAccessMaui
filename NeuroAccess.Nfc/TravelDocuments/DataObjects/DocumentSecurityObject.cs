using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.Pkcs;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using Waher.Runtime.Inventory;

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
		/// <param name="SignedData">Parsed content.</param>
		/// <param name="LdsSecurityObject">LDS Security Object.</param>
		public DocumentSecurityObject(byte[] Value, SignedCms? SignedData, LdsSecurityObject LdsSecurityObject)
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
		public SignedCms? SignedData { get; }

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

			if ((Client.AppInfo?.LdsVersion ?? 0) >= 1.8)
			{
				try
				{
					SignedCms SignedData = new();
					SignedData.Decode(Value);

					SignedData.CheckSignature(true);

					byte[]? Content = SignedData.ContentInfo?.Content;
					string? ContentOid = SignedData.ContentInfo?.ContentType?.Value;
					if (Content is null || string.IsNullOrEmpty(ContentOid))
						return false;

					ISecurityObject? SecurityObject = Types.FindBest<ISecurityObject, string>(ContentOid);
					if (SecurityObject is null)
					{
						Client.Warning("OID not recognized: " + ContentOid);
						return false;
					}

					if (SecurityObject is not LdsSecurityObject LdsSecurityObject)
						return false;

					ASN1.TryDecodeDER(Client, Content, out object? ParsedContent);

					if (ParsedContent is not Vector ContentVector)
						return false;

					if (!LdsSecurityObject.Configure(ContentVector))
						return false;

					Parsed = new DocumentSecurityObject(Value, SignedData, LdsSecurityObject);
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
