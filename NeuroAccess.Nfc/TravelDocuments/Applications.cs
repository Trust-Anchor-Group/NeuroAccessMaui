namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Travel Document Applications.
	/// </summary>
	public static class Applications
	{
		/// <summary>
		/// LDS1 eMRTD Application. §4 ICAO Doc 9303-10:
		/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
		/// </summary>
		public static readonly byte[] DF1 = [0xA0, 0x00, 0x00, 0x02, 0x47, 0x10, 0x01];
	}
}
