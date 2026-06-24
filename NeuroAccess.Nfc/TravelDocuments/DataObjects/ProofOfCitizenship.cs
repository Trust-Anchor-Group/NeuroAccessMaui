using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Proof of citizenship
	/// </summary>
	public class ProofOfCitizenship : DataObject
	{
		/// <summary>
		/// Proof of citizenship
		/// </summary>
		public ProofOfCitizenship()
			: base([])
		{
		}

		/// <summary>
		/// Proof of citizenship
		/// </summary>
		/// <param name="Value">Binary value.</param>
		public ProofOfCitizenship(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x5f16;

		/// <summary>
		/// JPEG image ([ISO/IEC 10918)
		/// </summary>
		public byte[] JPegImage => this.Value;

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
			Parsed = new ProofOfCitizenship(Value);
			return true;
		}
	}
}
