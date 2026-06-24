using System.Globalization;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Machine Readable Zone Information in DG1. Reference: §4.7.1, EF.DG1, ICAO Doc 9303-10, Table 39.
	/// </summary>
	public class MachineReadableZoneInformation : NestedDataObject
	{
		/// <summary>
		/// Machine Readable Zone Information in DG1. Reference: §4.7.1, EF.DG1, ICAO Doc 9303-10, Table 39.
		/// </summary>
		public MachineReadableZoneInformation()
			: base([])
		{
		}

		/// <summary>
		/// Machine Readable Zone Information in DG1. Reference: §4.7.1, EF.DG1, ICAO Doc 9303-10, Table 39.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Mrz">Parsed MRZ Information.</param>
		public MachineReadableZoneInformation(byte[] Value, MrzDataObject? Mrz)
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
					Client.Warning("Unknown DG1 tag: " + SubGroup.Tag.ToString("X4", CultureInfo.InvariantCulture));
					break;
				}
			}

			return new MachineReadableZoneInformation(Value, Mrz);
		}
	}
}
