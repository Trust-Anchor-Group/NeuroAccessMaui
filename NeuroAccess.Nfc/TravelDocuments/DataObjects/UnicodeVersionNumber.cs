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
		/// <param name="Version">Version number.</param>
		/// <param name="Release">Release</param>
		public UnicodeVersionNumber(byte[] Value, double Version, int Release)
			: base(Value)
		{
			this.Version = Version;
			this.Release = Release;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f36;

		/// <summary>
		/// Parsed version number.
		/// </summary>
		public double Version { get; }

		/// <summary>
		/// Release number.
		/// </summary>
		public int Release { get; }

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
				string MajorVersion = TrimLeadingZeroes(Encoding.ASCII.GetString(Value, 0, 2));
				string MinorVersion = TrimLeadingZeroes(Encoding.ASCII.GetString(Value, 2, 2));
				string ReleaseVersion = TrimLeadingZeroes(Encoding.ASCII.GetString(Value, 4, 2));

				Client.Information("Unicode version: " + MajorVersion + "." + MinorVersion +
					"." + ReleaseVersion);

				if (double.TryParse(MajorVersion + "." + MinorVersion,
					NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double d) &&
					int.TryParse(ReleaseVersion, NumberStyles.Integer, CultureInfo.InvariantCulture,
						out int i))
				{
					Parsed = new UnicodeVersionNumber(Value, d, i);
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
