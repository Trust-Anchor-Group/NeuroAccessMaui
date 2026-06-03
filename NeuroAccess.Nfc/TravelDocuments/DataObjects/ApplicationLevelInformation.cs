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
		/// <param name="LdsVersion">LDS Version.</param>
		/// <param name="UnicodeVersion">Unicode Version.</param>
		/// <param name="TagList">Supported data group tags.</param>
		public ApplicationLevelInformation(byte[] Value, double LdsVersion, double UnicodeVersion,
			TagList? TagList)
			: this(Value, LdsVersion, 0, 0, UnicodeVersion, TagList)
		{
		}

		/// <summary>
		/// Application-Level Information. Reference: §4.6.1, EF.COM, ICAO Doc 9303-10, Table 35.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="LdsVersion">LDS Version.</param>
		/// <param name="LdsMajorVersion">LDS major version number.</param>
		/// <param name="LdsMinorVersion">LDS minor version number.</param>
		/// <param name="UnicodeVersion">Unicode Version.</param>
		/// <param name="TagList">Supported data group tags.</param>
		public ApplicationLevelInformation(byte[] Value, double LdsVersion, int LdsMajorVersion,
			int LdsMinorVersion, double UnicodeVersion, TagList? TagList)
			: base(Value)
		{
			this.LdsVersion = LdsVersion;
			this.LdsMajorVersion = LdsMajorVersion;
			this.LdsMinorVersion = LdsMinorVersion;
			this.UnicodeVersion = UnicodeVersion;
			this.TagList = TagList;
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
		/// LDS major version number.
		/// </summary>
		public int LdsMajorVersion { get; }

		/// <summary>
		/// LDS minor version number.
		/// </summary>
		public int LdsMinorVersion { get; }

		/// <summary>
		/// Determines if the LDS version is at least the requested major and minor version.
		/// </summary>
		/// <param name="MajorVersion">Required major version number.</param>
		/// <param name="MinorVersion">Required minor version number.</param>
		/// <returns>If the LDS version is greater than or equal to the requested version.</returns>
		public bool IsLdsVersionAtLeast(int MajorVersion, int MinorVersion)
		{
			if (this.LdsMajorVersion == 0 && this.LdsMinorVersion == 0 && this.LdsVersion > 0)
			{
				double RequestedVersion = double.Parse(
					MajorVersion.ToString(CultureInfo.InvariantCulture) + "." +
					MinorVersion.ToString(CultureInfo.InvariantCulture),
					CultureInfo.InvariantCulture);

				return this.LdsVersion >= RequestedVersion;
			}

			if (this.LdsMajorVersion != MajorVersion)
				return this.LdsMajorVersion > MajorVersion;

			return this.LdsMinorVersion >= MinorVersion;
		}

		/// <summary>
		/// Unicode Version
		/// </summary>
		public double UnicodeVersion { get; }

		/// <summary>
		/// Supported tags
		/// </summary>
		public TagList? TagList { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <param name="Client">Client parsing objects.</param>
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

			return new ApplicationLevelInformation(Value, LdsVersion?.Version ?? 0,
				LdsVersion?.MajorVersion ?? 0, LdsVersion?.MinorVersion ?? 0,
				UnicodeVersion?.Version ?? 0, TagList);
		}
	}
}
