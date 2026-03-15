using System.Diagnostics.CodeAnalysis;

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
		/// <param name="Inner">Parsed content.</param>
		public DocumentSecurityObject(byte[] Value, object? Inner)
			: base(Value)
		{
			this.Inner = Inner;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x77;

		/// <summary>
		/// Parsed content.
		/// </summary>
		public object? Inner { get; }

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
			if ((Client.AppInfo?.LdsVersion ?? 0) >= 1.8 &&
				TravelDocumentsClient.TryDecodeDER(Value, out object? Inner))
			{
				Parsed = new DocumentSecurityObject(Value, Inner);
				return true;
			}
			else
			{
				// TODO: LDS version < 1.8 support

				Parsed = null;
				return false;
			}
		}
	}
}
