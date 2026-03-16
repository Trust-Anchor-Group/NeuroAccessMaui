namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Encoding. Reference: §4.7.2, EF.COM, ICAO Doc 9303-10, Table 43.
	/// </summary>
	public class BiometricEncoding : NestedDataObject
	{
		/// <summary>
		/// Biometric Encoding. Reference: §4.7.2, EF.COM, ICAO Doc 9303-10, Table 43.
		/// </summary>
		public BiometricEncoding()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Encoding. Reference: §4.7.2, EF.COM, ICAO Doc 9303-10, Table 43.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="LdsVersion">LDS Version</param>
		/// <param name="UnicodeVersion">Unicode Version</param>
		public BiometricEncoding(byte[] Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x75;

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			return new BiometricEncoding(Value);
		}
	}
}
