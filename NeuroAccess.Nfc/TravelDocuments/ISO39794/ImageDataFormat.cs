namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// ISO/IEC 39794-5 image data format code.
	/// </summary>
	public enum ImageDataFormat
	{
		/// <summary>
		/// Unknown or unsupported image data format.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// JPEG image data.
		/// </summary>
		Jpeg = 2,

		/// <summary>
		/// Lossy JPEG 2000 image data.
		/// </summary>
		Jpeg2000Lossy = 3,

		/// <summary>
		/// Lossless JPEG 2000 image data.
		/// </summary>
		Jpeg2000Lossless = 4
	}
}
