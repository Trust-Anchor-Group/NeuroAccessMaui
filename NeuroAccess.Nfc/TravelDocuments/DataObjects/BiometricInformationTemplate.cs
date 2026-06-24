namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Information Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
	/// </summary>
	public class BiometricInformationTemplate : NestedDataObject
	{
		/// <summary>
		/// Biometric Information Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
		/// </summary>
		public BiometricInformationTemplate()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Information Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="BiometricHeaderTemplate">Biometric header template.</param>
		/// <param name="BiometricDataBlock">Biometric data block</param>
		public BiometricInformationTemplate(byte[] Value, BiometricHeaderTemplate? BiometricHeaderTemplate,
			BiometricDataBlock? BiometricDataBlock)
			: base(Value)
		{
			this.BiometricHeaderTemplate = BiometricHeaderTemplate;
			this.BiometricDataBlock = BiometricDataBlock;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x7f60;

		/// <summary>
		/// Biometric header template
		/// </summary>
		public BiometricHeaderTemplate? BiometricHeaderTemplate { get; }

		/// <summary>
		/// Biometric data block.
		/// </summary>
		public BiometricDataBlock? BiometricDataBlock { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			BiometricHeaderTemplate? BiometricHeaderTemplate = null;
			BiometricDataBlock? BiometricDataBlock = null;

			foreach (IDataObject Object in Inner)
			{
				if (Object is BiometricHeaderTemplate BiometricHeaderTemplate2)
					BiometricHeaderTemplate = BiometricHeaderTemplate2;
				else if (Object is BiometricDataBlock BiometricDataBlock2)
					BiometricDataBlock = BiometricDataBlock2;
			}

			return new BiometricInformationTemplate(Value, BiometricHeaderTemplate, BiometricDataBlock);
		}
	}
}
