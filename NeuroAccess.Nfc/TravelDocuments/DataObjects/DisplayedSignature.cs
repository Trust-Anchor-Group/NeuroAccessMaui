using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Displayed Signatures or Usual Marks (DG7). Reference: §4.7.7, EF.DG7, ICAO Doc 9303-10, Table 62.
	/// </summary>
	public class DisplayedSignature : DataObject
	{
		/// <summary>
		/// Displayed Signatures or Usual Marks (DG7). Reference: §4.7.7, EF.DG7, ICAO Doc 9303-10, Table 62.
		/// </summary>
		public DisplayedSignature()
			: base([])
		{
		}

		/// <summary>
		/// Displayed Signatures or Usual Marks (DG7). Reference: §4.7.7, EF.DG7, ICAO Doc 9303-10, Table 62.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="LdsVersion">LDS Version</param>
		/// <param name="UnicodeVersion">Unicode Version</param>
		public DisplayedSignature(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f43;

		/// <summary>
		/// Raw image data of signature (May be JPEG or JPEG2000).
		/// </summary>
		public byte[] ImageData => this.Value;

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
			Parsed = new DisplayedSignature(Value);
			return true;
		}
	}
}
