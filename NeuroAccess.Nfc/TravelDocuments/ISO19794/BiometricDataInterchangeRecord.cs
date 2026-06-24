namespace NeuroAccess.Nfc.TravelDocuments.ISO19794
{
	/// <summary>
	/// Biometric Data Interchange Record (BDIR)
	/// </summary>
	public class BiometricDataInterchangeRecord
	{
		/// <summary>
		/// Biometric Data Interchange Record (BDIR)
		/// </summary>
		/// <param name="Representations">Biometric data representations.</param>
		public BiometricDataInterchangeRecord(Representation[] Representations)
		{
			this.Representations = Representations;
		}

		/// <summary>
		/// Biometric representations.
		/// </summary>
		public Representation[] Representations { get; }
	}
}
