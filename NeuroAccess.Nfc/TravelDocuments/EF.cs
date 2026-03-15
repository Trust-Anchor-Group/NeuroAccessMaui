namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Elementary Files in travel documents.
	/// </summary>
	public static class EF
	{
		/// <summary>
		/// EF.EntryRecords. §5.1.4 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort EntryRecords = 0x0101;

		/// <summary>
		/// EF.ExitRecords. §5.1.3 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort ExitRecords = 0x0102;

		/// <summary>
		/// EF.VisaRecords. §5.2.3 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort VisaRecords = 0x0103;

		/// <summary>
		/// EF.Certificates. §5.1.2 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort Certificates = 0x011a;

		/// <summary>
		/// EF.CardAccess. §3.11.3 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort CardAccess = 0x011c;

		/// <summary>
		/// EF.CardSecurity. §3.11.4 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort CardSecurity = 0x011d;

		/// <summary>
		/// Common Data (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort COM = 0x011e;

		/// <summary>
		/// Security Object Data (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort SOD = 0x011d;

		/// <summary>
		/// Data Group 1 (MRZ) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG1 = 0x0101;

		/// <summary>
		/// Data Group 2 (Encoded Identification Features — Face) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG2 = 0x0102;

		/// <summary>
		/// Data Group 3 (Additional Identification Feature — Finger(s)) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG3 = 0x0103;

		/// <summary>
		/// Data Group 4 (Additional Identification Feature — Iris(es)) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG4 = 0x0104;

		/// <summary>
		/// Data Group 5 (Displayed Portrait) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG5 = 0x0105;

		/// <summary>
		/// Data Group 11 (Additional Personal Detail(s)) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG11 = 0x010b;

		/// <summary>
		/// Data Group 12 (Additional Document Detail(s)) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG12 = 0x010c;

		/// <summary>
		/// Data Group 13 (Optional Details(s)) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG13 = 0x010d;

		/// <summary>
		/// Data Group 14 (Security Options) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG14 = 0x010e;

		/// <summary>
		/// Data Group 15 (Active Authentication Public Key Info) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG15 = 0x010f;

		/// <summary>
		/// Data Group 16 (Person(s) to Notify) (In LDS1 eMRTD Application)
		/// </summary>
		public const ushort DG16 = 0x0110;

		/// <summary>
		/// EF.Biometrics1-64 = 0x0201-0x0240. §5.3.3 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort BiometricsX = 0x0200;

		/// <summary>
		/// EF.DIR. §3.11.2 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort DIR = 0x2f00;

		/// <summary>
		/// EF.ATR. §3.11.1 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public const ushort ATR = 0x2f01;
	}
}
