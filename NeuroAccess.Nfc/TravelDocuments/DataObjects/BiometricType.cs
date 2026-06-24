using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Type
	/// </summary>
	public class BiometricType : DataObject
	{
		/// <summary>
		/// Biometric Type
		/// </summary>
		public BiometricType()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Type
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public BiometricType(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x81;

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
			Parsed = new BiometricType(Value);
			return true;
		}
	}
}
