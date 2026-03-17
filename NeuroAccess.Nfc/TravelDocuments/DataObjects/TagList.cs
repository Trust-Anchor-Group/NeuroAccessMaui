using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Tag List
	/// </summary>
	public class TagList : DataObject
	{
		private readonly Dictionary<ushort, bool> tags = [];

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
		public TagList(byte[] Value)
			: base(Value)
		{
			int i, c = Value.Length;
			ushort Tag;

			for (i = 0; i < c; i++)
			{
				Tag = Value[i];

				if ((Tag & 31) == 31 && ++i < c)
				{
					Tag <<= 8;
					Tag |= Value[i];
				}

				this.tags[Tag] = true;
			}
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5c;

		/// <summary>
		/// If a tag is supported.
		/// </summary>
		/// <param name="Tag">Tag</param>
		/// <returns>If the Tag is available in the list.</returns>
		public bool HasTag(ushort Tag) => this.tags.ContainsKey(Tag);

		/// <summary>
		/// If EF.COM is supported.
		/// </summary>
		public bool HasCommon => this.HasTag(0x60);

		/// <summary>
		/// If EF.SOD is supported.
		/// </summary>
		public bool HasSecurityObject => this.HasTag(0x77);

		/// <summary>
		/// Checks if a data group is supported.
		/// </summary>
		/// <param name="DataGroupNumber">Data Group number (DG1=1, ...)</param>
		/// <returns>If the corresponding data group is supported.</returns>
		public bool HasDataGroup(int DataGroupNumber)
		{
			// Ref §4.7, Table 38, ICAO 9303, Part 10.

			switch (DataGroupNumber)
			{
				case 1: return this.HasTag(0x61);
				case 2: return this.HasTag(0x75);
				case 3: return this.HasTag(0x63);
				case 4: return this.HasTag(0x76);
				case 5: return this.HasTag(0x65);
				case 6: return this.HasTag(0x66);
				case 7: return this.HasTag(0x67);
				case 8: return this.HasTag(0x68);
				case 9: return this.HasTag(0x69);
				case 10: return this.HasTag(0x6a);
				case 11: return this.HasTag(0x6b);
				case 12: return this.HasTag(0x6c);
				case 13: return this.HasTag(0x6d);
				case 14: return this.HasTag(0x6e);
				case 15: return this.HasTag(0x6f);
				case 16: return this.HasTag(0x70);
				default: return false;
			}
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
			Parsed = new TagList(Value);
			return true;
		}
	}
}
