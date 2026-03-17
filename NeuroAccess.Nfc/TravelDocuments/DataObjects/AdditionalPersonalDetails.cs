namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Additional Personal Details. Reference: §4.7.11, EF.DG11, ICAO Doc 9303-10, Table 71.
	/// </summary>
	public class AdditionalPersonalDetails : NestedDataObject
	{
		/// <summary>
		/// Additional Personal Details. Reference: §4.7.11, EF.DG11, ICAO Doc 9303-10, Table 71.
		/// </summary>
		public AdditionalPersonalDetails()
			: base([])
		{
		}

		/// <summary>
		/// Additional Personal Details. Reference: §4.7.11, EF.DG11, ICAO Doc 9303-10, Table 71.
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
		public AdditionalPersonalDetails(byte[] Value, TagList? TagList, FullName? FullName,
			OtherNames? OtherNames, PersonalNumber? PersonalNumber, DateOfBirth? DateOfBirth,
			PlaceOfBirth? PlaceOfBirth, PermanentAddress? PermanentAddress, Telephone? Telephone,
			Profession? Profession, Title? Title, PersonalSummary? PersonalSummary,
			ProofOfCitizenship? ProofOfCitizenship, OtherNumbers? OtherNumbers,
			CustodyInformation? CustodyInformation) : base(Value)
		{
			this.TagList = TagList;
			this.FullName = FullName?.StringValue;
			this.OtherNames = OtherNames?.Names;
			this.PersonalNumber = PersonalNumber?.StringValue;
			this.DateOfBirth = DateOfBirth?.StringValue;
			this.PlaceOfBirth = PlaceOfBirth?.StringValue;
			this.PermanentAddress = PermanentAddress?.StringValue;
			this.Telephone = Telephone?.StringValue;
			this.Profession = Profession?.StringValue;
			this.Title = Title?.StringValue;
			this.PersonalSummary = PersonalSummary?.StringValue;
			this.ProofOfCitizenship = ProofOfCitizenship;
			this.OtherNumbers = OtherNumbers?.NumberFields;
			this.CustodyInformation = CustodyInformation?.StringValue;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x6b;

		/// <summary>
		/// Tag List
		/// </summary>
		public TagList? TagList { get; }

		/// <summary>
		/// Full name
		/// </summary>
		public string? FullName { get; }

		/// <summary>
		/// Other names
		/// </summary>
		public string[]? OtherNames { get; }

		/// <summary>
		/// Personal number
		/// </summary>
		public string? PersonalNumber { get; }

		/// <summary>
		/// Date of birth
		/// </summary>
		public string? DateOfBirth { get; }

		/// <summary>
		/// Place of birth
		/// </summary>
		public string? PlaceOfBirth { get; }

		/// <summary>
		/// Permanent address
		/// </summary>
		public string? PermanentAddress { get; }

		/// <summary>
		/// Telephone
		/// </summary>
		public string? Telephone { get; }

		/// <summary>
		/// Profession
		/// </summary>
		public string? Profession { get; }

		/// <summary>
		/// Title
		/// </summary>
		public string? Title { get; }

		/// <summary>
		/// Personal Summary
		/// </summary>
		public string? PersonalSummary { get; }

		/// <summary>
		/// Proof of citizenship
		/// </summary>
		public ProofOfCitizenship? ProofOfCitizenship { get; }

		/// <summary>
		/// Other numbers
		/// </summary>
		public string[]? OtherNumbers { get; }

		/// <summary>
		/// Custody information
		/// </summary>
		public string? CustodyInformation { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			TagList? TagList = null;
			FullName? FullName = null;
			OtherNames? OtherNames = null;
			PersonalNumber? PersonalNumber = null;
			DateOfBirth? DateOfBirth = null;
			PlaceOfBirth? PlaceOfBirth = null;
			PermanentAddress? PermanentAddress = null;
			Telephone? Telephone = null;
			Profession? Profession = null;
			Title? Title = null;
			PersonalSummary? PersonalSummary = null;
			ProofOfCitizenship? ProofOfCitizenship = null;
			OtherNumbers? OtherNumbers = null;
			CustodyInformation? CustodyInformation = null;

			foreach (IDataObject Object in Inner)
			{
				if (Object is TagList TagList2)
					TagList = TagList2;
				else if (Object is FullName FullName2)
					FullName = FullName2;
				else if (Object is OtherNames OtherNames2)
					OtherNames = OtherNames2;
				else if (Object is PersonalNumber PersonalNumber2)
					PersonalNumber = PersonalNumber2;
				else if (Object is DateOfBirth DateOfBirth2)
					DateOfBirth = DateOfBirth2;
				else if (Object is PlaceOfBirth PlaceOfBirth2)
					PlaceOfBirth = PlaceOfBirth2;
				else if (Object is PermanentAddress PermanentAddress2)
					PermanentAddress = PermanentAddress2;
				else if (Object is Telephone Telephone2)
					Telephone = Telephone2;
				else if (Object is Profession Profession2)
					Profession = Profession2;
				else if (Object is Title Title2)
					Title = Title2;
				else if (Object is PersonalSummary PersonalSummary2)
					PersonalSummary = PersonalSummary2;
				else if (Object is ProofOfCitizenship ProofOfCitizenship2)
					ProofOfCitizenship = ProofOfCitizenship2;
				else if (Object is OtherNumbers OtherNumbers2)
					OtherNumbers = OtherNumbers2;
				else if (Object is CustodyInformation CustodyInformation2)
					CustodyInformation = CustodyInformation2;
			}

			return new AdditionalPersonalDetails(Value, TagList, FullName, OtherNames, PersonalNumber,
				DateOfBirth, PlaceOfBirth, PermanentAddress, Telephone, Profession, Title,
				PersonalSummary, ProofOfCitizenship, OtherNumbers, CustodyInformation);
		}
	}
}
