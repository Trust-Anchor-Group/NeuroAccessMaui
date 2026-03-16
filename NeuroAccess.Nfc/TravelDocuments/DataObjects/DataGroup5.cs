using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Data Group 5. Reference: §4.7.5, EF.DG5, ICAO Doc 9303-10, Table 58.
	/// </summary>
	public class DataGroup5 : NestedDataObject
	{
		/// <summary>
		/// Data Group 5. Reference: §4.7.5, EF.DG5, ICAO Doc 9303-10, Table 58.
		/// </summary>
		public DataGroup5()
			: base([])
		{
		}

		/// <summary>
		/// Data Group 5. Reference: §4.7.5, EF.DG5, ICAO Doc 9303-10, Table 58.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="InstanceCount">Instance count.</param>
		/// <param name="Photos">Photos.</param>
		public DataGroup5(byte[] Value, int InstanceCount, DisplayedPortrait[] Photos)
			: base(Value)
		{
			this.InstanceCount = InstanceCount;
			this.Photos = Photos;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x65;

		/// <summary>
		/// Instance count
		/// </summary>
		public int InstanceCount { get; }

		/// <summary>
		/// Photo
		/// </summary>
		public DisplayedPortrait[] Photos { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			ChunkedList<DisplayedPortrait> Photos = [];
			int Count = 0;

			foreach (IDataObject Object in Inner)
			{
				if (Object is NumberOfInstances NrInstances)
					Count = NrInstances.Count;
				else if (Object is DisplayedPortrait Photo)
					Photos.Add(Photo);
				else
				{
					Client.Warning("Unknown DG5 tag: " + Object.Tag.ToString("X4"));
					break;
				}
			}

			return new DataGroup5(Value, Count, [.. Photos]);
		}
	}
}
