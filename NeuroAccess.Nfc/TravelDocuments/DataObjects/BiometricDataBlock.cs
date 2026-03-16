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
		public BiometricDataBlock(byte[] Value)
			: base(Value)
		{
		}
	}
}
