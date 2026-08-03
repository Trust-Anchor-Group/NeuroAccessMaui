using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// LDS Version Number
	/// </summary>
	public class LdsVersionNumber : DataObject
	{
		/// <summary>
		/// LDS Version Number
		/// </summary>
		public LdsVersionNumber()
			: base([])
		{
		}

		/// <summary>
		/// LDS Version Number
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="MajorVersion">Major version number.</param>
		/// <param name="MinorVersion">Minor version number.</param>
		/// <param name="Version">Version string.</param>
		public LdsVersionNumber(byte[] Value, int MajorVersion, int MinorVersion, string Version)
			: base(Value)
		{
			this.MajorVersion = MajorVersion;
			this.MinorVersion = MinorVersion;
			this.Version = Version;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f01;

		/// <summary>
		/// Parsed major version number.
		/// </summary>
		public int MajorVersion { get; }

		/// <summary>
		/// Parsed minor version number.
		/// </summary>
		public int MinorVersion { get; }

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
			if (Value.Length == 4)
			{
				string VersionString = Encoding.ASCII.GetString(Value);
				string MajorVersion = TrimLeadingZeroes(VersionString[0..2]);
				string MinorVersion = TrimLeadingZeroes(VersionString[2..4]);

				Client.Information("LDS version: " + MajorVersion + "." + MinorVersion);

				if (int.TryParse(MajorVersion, out int MajorVersionInt) &&
					int.TryParse(MinorVersion, out int MinorVersionInt))
				{
					Parsed = new LdsVersionNumber(Value, MajorVersionInt, MinorVersionInt, VersionString);
					return true;
				}
			}
			else
				Client.Warning("LDS version: " + Hashes.BinaryToString(Value));

			Parsed = null;
			return false;
		}
	}
}
