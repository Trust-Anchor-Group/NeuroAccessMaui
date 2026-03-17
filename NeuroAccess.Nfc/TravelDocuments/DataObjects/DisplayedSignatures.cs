using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Displayed Signatures or Usual Marks (DG7). Reference: §4.7.7, EF.DG7, ICAO Doc 9303-10, Table 62.
	/// </summary>
	public class DisplayedSignatures : NestedDataObject
	{
		/// <summary>
		/// Displayed Signatures or Usual Marks (DG7). Reference: §4.7.7, EF.DG7, ICAO Doc 9303-10, Table 62.
		/// </summary>
		public DisplayedSignatures()
			: base([])
		{
		}

		/// <summary>
		/// Displayed Signatures or Usual Marks (DG7). Reference: §4.7.7, EF.DG7, ICAO Doc 9303-10, Table 62.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Signatures">Displayed signatures or usual marks.</param>
		public DisplayedSignatures(byte[] Value, DisplayedSignature[] Signatures)
			: base(Value)
		{
			this.Signatures = Signatures;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x67;

		/// <summary>
		/// Displayed signatures or usual marks.
		/// </summary>
		public DisplayedSignature[]? Signatures;

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			ChunkedList<DisplayedSignature> Signatures = [];

			foreach (IDataObject Object in Inner)
			{
				if (Object is DisplayedSignature DisplayedSignature)
					Signatures.Add(DisplayedSignature);
			}

			return new DisplayedSignatures(Value, [.. Signatures]);
		}
	}
}
