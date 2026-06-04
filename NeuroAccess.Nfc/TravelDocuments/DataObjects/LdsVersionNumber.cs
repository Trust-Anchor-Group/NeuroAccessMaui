using System.Diagnostics.CodeAnalysis;
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
		public LdsVersionNumber(byte[] Value, int MajorVersion, int MinorVersion)
			: base(Value)
		{
			this.MajorVersion = MajorVersion;
			this.MinorVersion = MinorVersion;
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
				string MajorVersion = TrimLeadingZeroes(Encoding.ASCII.GetString(Value, 0, 2));
				string MinorVersion = TrimLeadingZeroes(Encoding.ASCII.GetString(Value, 2, 2));

				Client.Information("LDS version: " + MajorVersion + "." + MinorVersion);

				if (int.TryParse(MajorVersion, out int MajorVersionInt) &&
					int.TryParse(MinorVersion, out int MinorVersionInt))
				{
					Parsed = new LdsVersionNumber(Value, MajorVersionInt, MinorVersionInt);
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
