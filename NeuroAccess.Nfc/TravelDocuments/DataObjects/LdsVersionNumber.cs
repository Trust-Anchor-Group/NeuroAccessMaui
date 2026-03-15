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
		/// <param name="Version">Version number.</param>
		public LdsVersionNumber(byte[] Value, double Version)
			: base(Value)
		{
			this.Version = Version;
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
				string MajorVersion = Encoding.ASCII.GetString(Value, 0, 2);
				string MinorVersion = Encoding.ASCII.GetString(Value, 2, 2);

				Client.Information("LDS version: " + MajorVersion.TrimStart('0') +
					"." + MinorVersion.TrimStart('0'));

				if (double.TryParse(MajorVersion + "." + MinorVersion, out double d))
				{
					Parsed = new LdsVersionNumber(Value, d);
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
