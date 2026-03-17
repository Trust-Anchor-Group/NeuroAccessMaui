using NeuroAccess.Nfc.TravelDocuments.ISO19794;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Abstract base class for biometric Data Blocks
	/// </summary>
	public abstract class BiometricDataBlock : DataObject
	{
		/// <summary>
		/// Abstract base class for biometric Data Blocks
		/// </summary>
		public BiometricDataBlock()
			: base([])
		{
		}

		/// <summary>
		/// Abstract base class for biometric Data Blocks
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Record">Biometric Data Interchange Record</param>
		public BiometricDataBlock(byte[] Value, BiometricDataInterchangeRecord? Record)
			: base(Value)
		{
			this.Record = Record;
		}

		/// <summary>
		/// Biomatric Data Interchange Record
		/// </summary>
		public BiometricDataInterchangeRecord? Record { get; }
	}
}
