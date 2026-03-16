using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Header Version Number
	/// </summary>
	public class HeaderVersion : DataObject
	{
		/// <summary>
		/// Header Version Number
		/// </summary>
		public HeaderVersion()
			: base([])
		{
		}

		/// <summary>
		/// Header Version Number
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Version">Version number.</param>
		public HeaderVersion(byte[] Value, double Version)
			: base(Value)
		{
			this.Version = Version;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x80;

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
			if (Value.Length == 2)
			{
				int MajorVersion = Value[0];
				int MinorVersion = Value[1];

				if (double.TryParse(MajorVersion + "." + MinorVersion, out double d))
				{
					Parsed = new HeaderVersion(Value, d);
					return true;
				}
			}

			Parsed = null;
			return false;
		}
	}
}
