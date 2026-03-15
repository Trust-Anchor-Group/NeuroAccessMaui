namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Data Group 2. Reference: §4.7.2, EF.DG2, ICAO Doc 9303-10, Table 44.
	/// </summary>
	public class DataGroup2 : NestedDataObject
	{
		/// <summary>
		/// Data Group 2. Reference: §4.7.2, EF.DG2, ICAO Doc 9303-10, Table 44.
		/// </summary>
		public DataGroup2()
			: base([])
		{
		}

		/// <summary>
		/// Data Group 2. Reference: §4.7.2, EF.DG2, ICAO Doc 9303-10, Table 44.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public DataGroup2(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x75;

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			return new DataGroup2(Value);
		}
	}
}
