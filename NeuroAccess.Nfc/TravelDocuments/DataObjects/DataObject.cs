using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Abstract base class for data objects.
	/// </summary>
	/// <param name="Value">Binary value.</param>
	public abstract class DataObject(byte[] Value) : IDataObject
	{
		/// <summary>
		/// Tag value.
		/// </summary>
		public abstract ushort Tag { get; }

		/// <summary>
		/// Binary value.
		/// </summary>
		public byte[] Value { get; } = Value;

		/// <summary>
		/// Tries to parse a binary representation of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <param name="Parsed">Parsed object, if successful.</param>
		/// <returns>If value could be parsed.</returns>
		public abstract bool TryParse(byte[] Value, TravelDocumentsClient Client,
			[NotNullWhen(true)] out IDataObject? Parsed);
	}
}
