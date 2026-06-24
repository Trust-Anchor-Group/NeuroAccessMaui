using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Binary Data Object. Used when no specific data object type is defined for a given tag.
	/// </summary>
	/// <param name="Tag">Tag value.</param>
	/// <param name="Value">Binary value of data object.</param>
	public class BinaryDataObject(ushort Tag, byte[] Value) : DataObject(Value)
	{
		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag { get; } = Tag;

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
			Parsed = null;
			return false;
		}
	}
}
