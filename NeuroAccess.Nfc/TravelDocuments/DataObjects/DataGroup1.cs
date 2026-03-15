namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Data Group 1. Reference: §4.7.1, EF.DG1, ICAO Doc 9303-10, Table 39.
	/// </summary>
	public class DataGroup1 : NestedDataObject
	{
		/// <summary>
		/// Data Group 1. Reference: §4.7.1, EF.DG1, ICAO Doc 9303-10, Table 39.
		/// </summary>
		public DataGroup1()
			: base([])
		{
		}

		/// <summary>
		/// Data Group 1. Reference: §4.7.1, EF.DG1, ICAO Doc 9303-10, Table 39.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="LdsVersion">LDS Version</param>
		/// <param name="UnicodeVersion">Unicode Version</param>
		public DataGroup1(byte[] Value, MrzDataObject? Mrz)
			: base(Value)
		{
			this.Mrz = Mrz;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x61;

		/// <summary>
		/// MRZ
		/// </summary>
		public MrzDataObject? Mrz { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			MrzDataObject? Mrz = null;

			foreach (IDataObject SubGroup in Inner)
			{
				if (SubGroup is MrzDataObject Mrz2)
					Mrz = Mrz2;
				else
				{
					Client.Warning("Unknown DG1 tag: " + SubGroup.Tag.ToString("X4"));
					break;
				}
			}

			return new DataGroup1(Value, Mrz);
		}
	}
}
