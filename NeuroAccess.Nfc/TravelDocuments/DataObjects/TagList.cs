using System;
using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Tag List
	/// </summary>
	public class TagList : DataObject
	{
		private readonly bool[] hasDataGroup;

		/// <summary>
		/// Tag List
		/// </summary>
		public TagList()
			: base([])
		{
		}

		/// <summary>
		/// Unicode Version Number
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="HasCommon">If EF.COM is supported.</param>
		/// <param name="HasSecurityObject">If EF.SOD is supported.</param>
		/// <param name="HasDataGroup">If corresponding data group is supported.</param>
		public TagList(byte[] Value, bool HasCommon, bool HasSecurityObject, bool[] HasDataGroup)
			: base(Value)
		{
			if (HasDataGroup.Length != 16)
				throw new ArgumentException("Invalid number of data groups.");

			this.HasCommon = HasCommon;
			this.HasSecurityObject = HasSecurityObject;
			this.hasDataGroup = HasDataGroup;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5c;

		/// <summary>
		/// If EF.COM is supported.
		/// </summary>
		public bool HasCommon { get; }

		/// <summary>
		/// If EF.SOD is supported.
		/// </summary>
		public bool HasSecurityObject { get; }

		/// <summary>
		/// Checks if a data group is supported.
		/// </summary>
		/// <param name="DataGroupNumber">Data Group number (DG1=1, ...)</param>
		/// <returns>If the corresponding data group is supported.</returns>
		public bool HasDataGroup(int DataGroupNumber)
		{
			if (DataGroupNumber < 1 || DataGroupNumber > this.hasDataGroup.Length)
				return false;

			return this.hasDataGroup[DataGroupNumber - 1];
		}

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
			// Ref §4.7, Table 38, ICAO 9303, Part 10.

			bool HasCommon = false;
			bool HasSecurityObject = false;
			bool[] HasDataGroup = new bool[16];

			foreach (byte Tag in Value)
			{
				switch (Tag)
				{
					case 0x60:
						HasCommon = true;
						Client.Information("EF.COM supported.");
						break;

					case 0x61:
						HasDataGroup[0] = true;
						Client.Information("EF.DG1 (MRZ) supported.");
						break;

					case 0x75:
						HasDataGroup[1] = true;
						Client.Information("EF.DG2 (Encoded Identification Features — Face) supported.");
						break;

					case 0x63:
						HasDataGroup[2] = true;
						Client.Information("EF.DG3 (Additional Identification Feature — Finger(s)) supported.");
						break;

					case 0x76:
						HasDataGroup[3] = true;
						Client.Information("EF.DG4 (Additional Identification Feature — Iris(es)) supported.");
						break;

					case 0x65:
						HasDataGroup[4] = true;
						Client.Information("EF.DG5 (Displayed Portrait) supported.");
						break;

					case 0x66:
						HasDataGroup[5] = true;
						Client.Information("EF.DG6 (Reserved) supported.");
						break;

					case 0x67:
						HasDataGroup[6] = true;
						Client.Information("EF.DG7 (Displayed Signature or Usual Mark) supported.");
						break;

					case 0x68:
						HasDataGroup[7] = true;
						Client.Information("EF.DG8 (Data Feature(s)) supported.");
						break;

					case 0x69:
						HasDataGroup[8] = true;
						Client.Information("EF.DG9 (Structure Feature(s)) supported.");
						break;

					case 0x6a:
						HasDataGroup[9] = true;
						Client.Information("EF.DG10 (Substance Feature(s)) supported.");
						break;

					case 0x6b:
						HasDataGroup[10] = true;
						Client.Information("EF.DG11 (Additional Personal Detail(s)) supported.");
						break;

					case 0x6c:
						HasDataGroup[11] = true;
						Client.Information("EF.DG12 (Additional Document Detail(s)) supported.");
						break;

					case 0x6d:
						HasDataGroup[12] = true;
						Client.Information("EF.DG13 (Optional Details(s)) supported.");
						break;

					case 0x6e:
						HasDataGroup[13] = true;
						Client.Information("EF.DG14 (Security Options) supported.");
						break;

					case 0x6f:
						HasDataGroup[14] = true;
						Client.Information("EF.DG15 (Active Authentication Public Key Info) supported.");
						break;

					case 0x70:
						HasDataGroup[15] = true;
						Client.Information("EF.DG16 (Person(s) to Notify) supported.");
						break;

					case 0x77:
						HasSecurityObject = true;
						Client.Information("EF.SOD supported.");
						break;

					default:
						Client.Warning("Unrecognized tag: " + Tag.ToString("X2"));
						break;
				}
			}

			Parsed = new TagList(Value, HasCommon, HasSecurityObject, HasDataGroup);
			return true;
		}
	}
}
