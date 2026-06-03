using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
		/// <param name="Version">Version number.</param>
		/// <param name="MajorVersion">Major version number.</param>
		/// <param name="MinorVersion">Minor version number.</param>
		public LdsVersionNumber(byte[] Value, double Version, int MajorVersion, int MinorVersion)
			: base(Value)
		{
			this.Version = Version;
			this.MajorVersion = MajorVersion;
			this.MinorVersion = MinorVersion;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f01;

		/// <summary>
		/// Parsed version number.
		/// </summary>
		public double Version { get; }

		/// <summary>
		/// Major version number.
		/// </summary>
		public int MajorVersion { get; }

		/// <summary>
		/// Minor version number.
		/// </summary>
		public int MinorVersion { get; }

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
				string MajorVersionText = TrimLeadingZeroes(Encoding.ASCII.GetString(Value, 0, 2));
				string MinorVersionText = TrimLeadingZeroes(Encoding.ASCII.GetString(Value, 2, 2));

				Client.Information("LDS version: " + MajorVersionText + "." + MinorVersionText);

				if (double.TryParse(MajorVersionText + "." + MinorVersionText,
					NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double Version) &&
					int.TryParse(MajorVersionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int MajorVersion) &&
					int.TryParse(MinorVersionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int MinorVersion))
				{
					Parsed = new LdsVersionNumber(Value, Version, MajorVersion, MinorVersion);
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
