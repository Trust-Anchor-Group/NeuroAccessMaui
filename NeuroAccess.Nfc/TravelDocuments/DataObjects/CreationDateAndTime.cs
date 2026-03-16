using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Creation Date and Time.
	/// </summary>
	public class CreationDateAndTime : DataObject
	{
		/// <summary>
		/// Creation Date and Time.
		/// </summary>
		public CreationDateAndTime()
			: base([])
		{
		}

		/// <summary>
		/// Creation Date and Time.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public CreationDateAndTime(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x83;

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
			// TODO: Parse
			Parsed = new CreationDateAndTime(Value);
			return true;
		}
	}
}
