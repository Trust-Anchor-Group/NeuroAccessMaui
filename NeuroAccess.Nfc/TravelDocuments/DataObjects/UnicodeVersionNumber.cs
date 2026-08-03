using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Unicode Version Number
	/// </summary>
	public class UnicodeVersionNumber : DataObject
	{
		/// <summary>
		/// Unicode Version Number
		/// </summary>
		public UnicodeVersionNumber()
			: base([])
		{
		}

		/// <summary>
		/// Unicode Version Number
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="MajorVersion">Major version number.</param>
		/// <param name="MinorVersion">Minor version number.</param>
		/// <param name="Release">Release</param>
		public UnicodeVersionNumber(byte[] Value, int MajorVersion, int MinorVersion, int Release,
			string Version)
			: base(Value)
		{
			this.MajorVersion = MajorVersion;
			this.MinorVersion = MinorVersion;
			this.Release = Release;
			this.Version = Version;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f36;

		/// <summary>
		/// Parsed major version number.
		/// </summary>
		public int MajorVersion { get; }

		/// <summary>
		/// Parsed minor version number.
		/// </summary>
		public int MinorVersion { get; }

		/// <summary>
		/// Release number.
		/// </summary>
		public int Release { get; }

		/// <summary>
		/// Version string.
		/// </summary>
		public string? Version { get; }

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
			if (Value.Length == 6)
			{
				string VersionString = Encoding.ASCII.GetString(Value);
				string MajorVersion = TrimLeadingZeroes(VersionString[0..2]);
				string MinorVersion = TrimLeadingZeroes(VersionString[2..4]);
				string ReleaseVersion = TrimLeadingZeroes(VersionString[4..6]);

				Client.Information("Unicode version: " + MajorVersion + "." + MinorVersion +
					"." + ReleaseVersion);

				if (int.TryParse(MajorVersion, out int MajorVersionInt) &&
					int.TryParse(MinorVersion, out int MinorVersionInt) &&
					int.TryParse(ReleaseVersion, out int ReleaseVersionInt))
				{
					Parsed = new UnicodeVersionNumber(Value, MajorVersionInt, MinorVersionInt,
						ReleaseVersionInt, VersionString);
					return true;
				}
			}
			else
				Client.Warning("Unicode version: " + Hashes.BinaryToString(Value));

			Parsed = null;
			return false;
		}
	}
}
