namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Biometric Header Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
	/// </summary>
	public class BiometricHeaderTemplate : NestedDataObject
	{
		/// <summary>
		/// Biometric Header Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
		/// </summary>
		public BiometricHeaderTemplate()
			: base([])
		{
		}

		/// <summary>
		/// Biometric Header Template. Reference: §4.7.2.1, EF.COM, ICAO Doc 9303-10, Table 44.
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="HeaderVersion">Header version</param>
		/// <param name="BiometricType">Biometric type</param>
		/// <param name="BiometricSubType">Biometric sub-type</param>
		/// <param name="CreationDateAndTime">Creation date and time</param>
		/// <param name="ValidityPeriod">Validity period</param>
		/// <param name="CreatorPid">Creator</param>
		/// <param name="FormatOwner">Format Owner</param>
		/// <param name="FormatType">Format Type</param>
		public BiometricHeaderTemplate(byte[] Value, HeaderVersion? HeaderVersion,
			BiometricType? BiometricType, BiometricSubType? BiometricSubType,
			CreationDateAndTime? CreationDateAndTime, ValidityPeriod? ValidityPeriod,
			CreatorPid? CreatorPid, FormatOwner? FormatOwner, FormatType? FormatType)
			: base(Value)
		{
			this.HeaderVersion = HeaderVersion;
			this.BiometricType = BiometricType;
			this.BiometricSubType = BiometricSubType;
			this.CreationDateAndTime = CreationDateAndTime;
			this.ValidityPeriod = ValidityPeriod;
			this.CreatorPid = CreatorPid;
			this.FormatOwner = FormatOwner;
			this.FormatType = FormatType;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0xa1;

		/// <summary>
		/// Header version.
		/// </summary>
		public HeaderVersion? HeaderVersion { get; }

		/// <summary>
		/// Biometric type
		/// </summary>
		public BiometricType? BiometricType { get; }

		/// <summary>
		/// Biometric sub-type
		/// </summary>
		public BiometricSubType? BiometricSubType { get; }

		/// <summary>
		/// Creation date and time
		/// </summary>
		public CreationDateAndTime? CreationDateAndTime { get; }

		/// <summary>
		/// Validity period
		/// </summary>
		public ValidityPeriod? ValidityPeriod { get; }

		/// <summary>
		/// Creator
		/// </summary>
		public CreatorPid? CreatorPid { get; }

		/// <summary>
		/// Format Owner
		/// </summary>
		public FormatOwner? FormatOwner { get; }

		/// <summary>
		/// Format Type
		/// </summary>
		public FormatType? FormatType { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			HeaderVersion? HeaderVersion = null;
			BiometricType? BiometricType = null;
			BiometricSubType? BiometricSubType = null;
			CreationDateAndTime? CreationDateAndTime = null;
			ValidityPeriod? ValidityPeriod = null;
			CreatorPid? CreatorPid = null;
			FormatOwner? FormatOwner = null;
			FormatType? FormatType = null;

			foreach (IDataObject Object in Inner)
			{
				if (Object is HeaderVersion HeaderVersion2)
					HeaderVersion = HeaderVersion2;
				else if (Object is BiometricType BiometricType2)
					BiometricType = BiometricType2;
				else if (Object is BiometricSubType BiometricSubType2)
					BiometricSubType = BiometricSubType2;
				else if (Object is CreationDateAndTime CreationDateAndTime2)
					CreationDateAndTime = CreationDateAndTime2;
				else if (Object is ValidityPeriod ValidityPeriod2)
					ValidityPeriod = ValidityPeriod2;
				else if (Object is CreatorPid CreatorPid2)
					CreatorPid = CreatorPid2;
				else if (Object is FormatOwner FormatOwner2)
					FormatOwner = FormatOwner2;
				else if (Object is FormatType FormatType2)
					FormatType = FormatType2;
			}

			return new BiometricHeaderTemplate(Value, HeaderVersion, BiometricType, BiometricSubType,
				CreationDateAndTime, ValidityPeriod, CreatorPid, FormatOwner, FormatType);
		}
	}
}
