using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Information Template Group Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
	/// </summary>
	public class BiometricInformationTemplates : NestedDataObject
	{
		/// <summary>
		/// Biometric Information Template Group Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
		/// </summary>
		public BiometricInformationTemplates()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Information Template Group Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="NrInstances">Number of templates.</param>
		/// <param name="Templates">Biometric information templates</param>
		public BiometricInformationTemplates(byte[] Value, int NrInstances,
			BiometricInformationTemplate[] Templates)
			: base(Value)
		{
			this.NrInstances = NrInstances;
			this.Templates = Templates;
		}

		/// <summary>
		/// Number of templates.
		/// </summary>
		public int NrInstances { get; }

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x7f61;

		/// <summary>
		/// Biometric information templates
		/// </summary>
		public BiometricInformationTemplate[]? Templates { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			NumberOfInstances? NumberOfInstances = null;
			ChunkedList<BiometricInformationTemplate> Templates = [];

			foreach (IDataObject Object in Inner)
			{
				if (Object is NumberOfInstances NumberOfInstances2)
					NumberOfInstances = NumberOfInstances2;
				else if (Object is BiometricInformationTemplate Template)
					Templates.Add(Template);
			}

			return new BiometricInformationTemplates(Value, NumberOfInstances?.Count ?? 0, [.. Templates]);
		}
	}
}
