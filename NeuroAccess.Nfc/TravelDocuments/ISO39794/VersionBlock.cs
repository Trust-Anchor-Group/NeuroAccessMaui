namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// Contains the ISO/IEC 39794 version generation and publication year.
	/// </summary>
	public class VersionBlock
	{
		/// <summary>
		/// Contains the ISO/IEC 39794 version generation and publication year.
		/// </summary>
		/// <param name="Generation">Version generation.</param>
		/// <param name="Year">Version publication year.</param>
		public VersionBlock(int Generation, int Year)
		{
			this.Generation = Generation;
			this.Year = Year;
		}

		/// <summary>
		/// Version generation.
		/// </summary>
		public int Generation { get; }

		/// <summary>
		/// Version publication year.
		/// </summary>
		public int Year { get; }
	}
}
