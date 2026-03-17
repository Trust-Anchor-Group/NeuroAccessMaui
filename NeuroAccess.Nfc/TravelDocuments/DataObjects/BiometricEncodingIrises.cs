namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Encoding of DG4. Reference: §4.7.4, EF.DG4, ICAO Doc 9303-10, Table 53.
	/// </summary>
	public class BiometricEncodingIrises : NestedDataObject
	{
		/// <summary>
		/// Biometric Encoding of DG4. Reference: §4.7.4, EF.DG4, ICAO Doc 9303-10, Table 53.
		/// </summary>
		public BiometricEncodingIrises()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Encoding of DG4. Reference: §4.7.4, EF.DG4, ICAO Doc 9303-10, Table 53.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Templates">Biometric information templates.</param>
		public BiometricEncodingIrises(byte[] Value, BiometricInformationTemplates? Templates)
			: base(Value)
		{
			this.Templates = Templates;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x76;

		/// <summary>
		/// Biometric information templates.
		/// </summary>
		public BiometricInformationTemplates? Templates;

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			BiometricInformationTemplates? BiometricInformationTemplates = null;

			foreach (IDataObject Object in Inner)
			{
				if (Object is BiometricInformationTemplates BiometricInformationTemplates2)
					BiometricInformationTemplates = BiometricInformationTemplates2;
			}

			return new BiometricEncodingIrises(Value, BiometricInformationTemplates);
		}
	}
}
