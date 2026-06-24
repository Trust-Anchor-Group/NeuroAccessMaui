using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Sub-Type
	/// </summary>
	public class BiometricSubType : DataObject
	{
		/// <summary>
		/// Biometric Sub-Type
		/// </summary>
		public BiometricSubType()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Sub-Type
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public BiometricSubType(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x82;

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
			Parsed = new BiometricSubType(Value);
			return true;
		}
	}
}
