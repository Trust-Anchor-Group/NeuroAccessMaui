namespace NeuroAccess.Nfc.TravelDocuments.ISO19794
{
	/// <summary>
	/// ICAO 9303 / ISO 19794-5 image data type.
	/// </summary>
	public enum ImageDataType
	{
		/// <summary>
		/// Uncompressed raster (rare, but defined)
		/// </summary>
		UncompressedRaster = 0,

		/// <summary>
		/// JPEG image
		/// </summary>
		Jpeg = 1,

		/// <summary>
		/// JPEG-2000 image
		/// </summary>
		Jpeg2000 = 2,

		/// <summary>
		/// PNG image
		/// </summary>
		Png = 3
	}
}
