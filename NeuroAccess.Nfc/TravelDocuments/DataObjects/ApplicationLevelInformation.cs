using System.Globalization;

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
		/// <param name="LdsMajorVersion">LDS Major Version</param>
		/// <param name="LdsMinorVersion">LDS Minor Version</param>
		/// <param name="UnicodeMajorVersion">Unicode Major Version</param>
		/// <param name="UnicodeMinorVersion">Unicode Minor Version</param>
		public ApplicationLevelInformation(byte[] Value, int LdsMajorVersion,
			int LdsMinorVersion, int UnicodeMajorVersion, int UnicodeMinorVersion,
			TagList? TagList)
			: base(Value)
		{
			this.LdsMajorVersion = LdsMajorVersion;
			this.LdsMinorVersion = LdsMinorVersion;
			this.UnicodeMajorVersion = UnicodeMajorVersion;
			this.UnicodeMinorVersion = UnicodeMinorVersion;
			this.TagList = TagList;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x60;

		/// <summary>
		/// LDS Major Version
		/// </summary>
		public int LdsMajorVersion { get; }

		/// <summary>
		/// LDS Minor Version
		/// </summary>
		public int LdsMinorVersion { get; }

		/// <summary>
		/// Unicode Major Version
		/// </summary>
		public int UnicodeMajorVersion { get; }

		/// <summary>
		/// Unicode Minor Version
		/// </summary>
		public int UnicodeMinorVersion { get; }

		/// <summary>
		/// Supported tags
		/// </summary>
		public TagList? TagList { get; }

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
					Client.Warning("Unknown application level information tag: " + SubGroup.Tag.ToString("X4", CultureInfo.InvariantCulture));
					break;
				}
			}

			return new ApplicationLevelInformation(Value, LdsVersion?.MajorVersion ?? 0,
				LdsVersion?.MinorVersion ?? 0, UnicodeVersion?.MajorVersion ?? 0,
				UnicodeVersion?.MinorVersion ?? 0, TagList);
		}

		/// <summary>
		/// Checks that the LDS version is at least the specified version.
		/// </summary>
		/// <param name="Major">Major version number.</param>
		/// <param name="Minor">Minor version number.</param>
		/// <returns>True if the LDS version is at least the specified version,
		/// otherwise false.</returns>
		public bool HasAtLeastLdsVersion(int Major, int Minor)
		{
			return this.LdsMajorVersion > Major ||
				(this.LdsMajorVersion == Major && this.LdsMinorVersion >= Minor);
		}
	}
}
