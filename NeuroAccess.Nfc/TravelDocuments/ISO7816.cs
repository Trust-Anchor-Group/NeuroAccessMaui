namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// First byte of a status word.
	/// </summary>
	public enum Iso7816StatusCategory : byte
	{
		/// <summary>
		/// No further qualification
		/// </summary>
		Ok = 0x90,

		/// <summary>
		/// SW2 encodes the number of data bytes still available
		/// </summary>
		DataStillAvailable = 0x61,

		/// <summary>
		/// Warning, state of non-volatile memory is unchanged
		/// </summary>
		WarningUnchanged = 0x62,

		/// <summary>
		/// Warning, state of non-volatile memory has changed
		/// </summary>
		WarningChanged = 0x63,

		/// <summary>
		/// Error, state of non-volatile memory is unchanged
		/// </summary>
		ErrorUnchanged = 0x64,

		/// <summary>
		/// Error, state of non-volatile memory has changed
		/// </summary>
		ErrorChanged = 0x65,

		/// <summary>
		/// Security-related issues
		/// </summary>
		SecurityIssue = 0x66,

		/// <summary>
		/// Wrong length
		/// </summary>
		WrongLength = 0x67,

		/// <summary>
		/// Functions in CLA not supported
		/// </summary>
		FunctionNotSupported = 0x68,

		/// <summary>
		/// Command not allowed
		/// </summary>
		NotAllowed = 0x69,

		/// <summary>
		/// Wrong parameters P1-P2
		/// </summary>
		WrongParameters = 0x6A,

		/// <summary>
		/// Wrong parameters P1-P2 
		/// </summary>
		WrongParameters2 = 0x6B,

		/// <summary>
		/// Wrong Le field; SW2 encodes the exact number of available data bytes
		/// </summary>
		WrongLeField = 0x6C,

		/// <summary>
		/// Instruction code not supported or invalid 
		/// </summary>
		InstructionCodeInvalid = 0x6D,

		/// <summary>
		/// Class not supported
		/// </summary>
		ClassNotSupported = 0x6E,

		/// <summary>
		/// No precise diagnosis 
		/// </summary>
		Other = 0x6f
	}

	/// <summary>
	/// Static class with extensions related to the ISO/IEC 7816 standard for
	/// Identification cards — Integrated circuit cards.
	/// </summary>
	public static class ISO_7816
	{
		public static class Classes
		{
			/// <summary>
			/// Standard Command, Basic Channel, No Secure Messaging.
			/// </summary>
			public const byte Basic = 0x00;

			/// <summary>
			/// Secure messaging.
			/// </summary>
			public const byte SecureMessaging = 0x0C;

			/// <summary>
			/// Command Chaining.
			/// </summary>
			public const byte Chaining = 0x10;
		}

		/// <summary>
		/// ISO
		/// </summary>
		public static class Instructions
		{
			public const byte DeactivateFile = 0x04;
			public const byte EraseRecord = 0x0C;
			public const byte EraseBinary = 0x0E;
			public const byte MessageSecurityEnvironment = 0x22;
			public const byte ManageChannel = 0x70;
			public const byte ExternalAuthenticate = 0x82;
			public const byte GetChallenge = 0x84;
			public const byte GeneralAuthenticate = 0x86;
			public const byte InternalAuthenticate = 0x88;
			public const byte Select = 0xA4;
			public const byte ReadBinary = 0xB0;
			public const byte ReadRecord = 0xB2;
			public const byte GetResponse = 0xC0;
			public const byte GetData = 0xCA;
			public const byte PutData = 0xDA;
			public const byte UpdateBinary = 0xD6;
			public const byte UpdateRecord = 0xDC;
		}


	}
}
