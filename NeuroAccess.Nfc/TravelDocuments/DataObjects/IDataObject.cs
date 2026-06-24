using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Decoded Data Object.
	/// </summary>
	/// <param name="Tag">Tag value.</param>
	/// <param name="Value">Binary value of data object.</param>
	public interface IDataObject
	{
		/// <summary>
		/// Tag value.
		/// </summary>
		ushort Tag { get; }

		/// <summary>
		/// Binary value.
		/// </summary>
		byte[] Value { get; }

		/// <summary>
		/// Tries to parse a binary representation of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <param name="Parsed">Parsed object, if successful.</param>
		/// <returns>If value could be parsed.</returns>
		bool TryParse(byte[] Value, TravelDocumentsClient Client, [NotNullWhen(true)] out IDataObject? Parsed);
	}
}
