using Waher.Runtime.Collections;

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
		public BiometricEncoding(byte[] Value, BiometricInformationTemplates? Templates)
			: base(Value)
		{
			this.Templates = Templates;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x75;

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

			return new BiometricEncoding(Value, BiometricInformationTemplates);
		}
	}
}
