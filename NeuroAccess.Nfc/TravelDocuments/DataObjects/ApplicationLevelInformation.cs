namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Application-Level Information. Reference: §4.6.1, EF.COM, ICAO Doc 9303-10, Table 35.
	/// </summary>
	public class ApplicationLevelInformation : NestedDataObject
	{
		/// <summary>
		/// Application-Level Information. Reference: §4.6.1, EF.COM, ICAO Doc 9303-10, Table 35.
		/// </summary>
		public ApplicationLevelInformation()
			: base([])
		{
		}

		/// <summary>
		/// Application-Level Information. Reference: §4.6.1, EF.COM, ICAO Doc 9303-10, Table 35.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="LdsVersion">LDS Version</param>
		/// <param name="UnicodeVersion">Unicode Version</param>
		public ApplicationLevelInformation(byte[] Value, double LdsVersion, double UnicodeVersion)
			: base(Value)
		{
			this.LdsVersion = LdsVersion;
			this.UnicodeVersion = UnicodeVersion;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x60;

		/// <summary>
		/// LDS Version
		/// </summary>
		public double LdsVersion { get; }

		/// <summary>
		/// Unicode Version
		/// </summary>
		public double UnicodeVersion { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			LdsVersionNumber? LdsVersion = null;
			UnicodeVersionNumber? UnicodeVersion = null;
			TagList? TagList = null;

			foreach (IDataObject SubGroup in Inner)
			{
				if (SubGroup is LdsVersionNumber LdsVersion2)
					LdsVersion = LdsVersion2;
				else if (SubGroup is UnicodeVersionNumber UnicodeVersion2)
					UnicodeVersion = UnicodeVersion2;
				else if (SubGroup is TagList TagList2)
					TagList = TagList2;
				else
				{
					Client.Warning("Unknown application level information tag: " + SubGroup.Tag.ToString("X4"));
					break;
				}
			}

			return new ApplicationLevelInformation(Value, LdsVersion?.Version ?? 0, UnicodeVersion?.Version ?? 0);
		}
	}
}
