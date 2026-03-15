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
