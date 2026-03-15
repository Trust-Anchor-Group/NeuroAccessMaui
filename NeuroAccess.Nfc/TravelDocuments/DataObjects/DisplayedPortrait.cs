using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Displayed portrait
	/// </summary>
	public class DisplayedPortrait : DataObject
	{
		/// <summary>
		/// Displayed portrait
		/// </summary>
		public DisplayedPortrait()
			: base([])
		{
		}

		/// <summary>
		/// Displayed portrait
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public DisplayedPortrait(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f40;

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
			Parsed = new DisplayedPortrait(Value);
			return true;
		}
	}
}
