using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Data Group 11. Reference: §4.7.11, EF.DG11, ICAO Doc 9303-10, Table 71.
	/// </summary>
	public class DataGroup11 : NestedDataObject
	{
		/// <summary>
		/// Data Group 11. Reference: §4.7.11, EF.DG11, ICAO Doc 9303-10, Table 71.
		/// </summary>
		public DataGroup11()
			: base([])
		{
		}

		/// <summary>
		/// Data Group 11. Reference: §4.7.11, EF.DG11, ICAO Doc 9303-10, Table 71.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="InstanceCount">Instance count.</param>
		/// <param name="Photos">Photos.</param>
		public DataGroup11(byte[] Value, string? FullName, string? PersonalNumber)
			: base(Value)
		{
			this.FullName = FullName;
			this.PersonalNumber = PersonalNumber;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x611;

		/// <summary>
		/// Full Name
		/// </summary>
		public string? FullName { get; }

		/// <summary>
		/// Full Name
		/// </summary>
		public string? PersonalNumber { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			string? FullName = null;
			string? PersonalNumber = null;

			foreach (IDataObject Object in Inner)
			{
				if (Object is FullName FullName2)
					FullName = FullName2.StringValue;
				else if (Object is PersonalNumber PersonalNumber2)
					PersonalNumber = PersonalNumber2.StringValue;
				else
				{
					Client.Warning("Unknown DG11 tag: " + Object.Tag.ToString("X4"));
					break;
				}
			}

			return new DataGroup11(Value, FullName, PersonalNumber);
		}
	}
}
