using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Validity Period.
	/// </summary>
	public class ValidityPeriod : DataObject
	{
		/// <summary>
		/// Validity Period.
		/// </summary>
		public ValidityPeriod()
			: base([])
		{
		}

		/// <summary>
		/// Validity Period.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public ValidityPeriod(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x85;

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
			Parsed = new ValidityPeriod(Value);
			return true;
		}
	}
}
