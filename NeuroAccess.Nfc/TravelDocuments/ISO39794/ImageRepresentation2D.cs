namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// ISO/IEC 39794-5 2D image representation block.
	/// </summary>
	public class ImageRepresentation2D
	{
		/// <summary>
		/// ISO/IEC 39794-5 2D image representation block.
		/// </summary>
		/// <param name="ImageData">Encoded image data.</param>
		/// <param name="ImageInformation">Image information block.</param>
		/// <param name="ExtensionData">Raw encoded extension data not interpreted by this parser.</param>
		public ImageRepresentation2D(byte[] ImageData, ImageInformation2D ImageInformation,
			byte[][] ExtensionData)
		{
			this.ImageData = ImageData;
			this.ImageInformation = ImageInformation;
			this.ExtensionData = ExtensionData;
		}

		/// <summary>
		/// Encoded image data.
		/// </summary>
		public byte[] ImageData { get; }

		/// <summary>
		/// Image information block.
		/// </summary>
		public ImageInformation2D ImageInformation { get; }

		/// <summary>
		/// Raw encoded extension data not interpreted by this parser.
		/// </summary>
		public byte[][] ExtensionData { get; }
	}
}
