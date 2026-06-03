namespace NeuroAccess.Nfc.TravelDocuments.ISO39794
{
	/// <summary>
	/// ISO/IEC 39794-5 2D image information block.
	/// </summary>
	public class ImageInformation2D
	{
		/// <summary>
		/// ISO/IEC 39794-5 2D image information block.
		/// </summary>
		/// <param name="ImageDataFormat">Image data format.</param>
		/// <param name="FaceImageKind">2D face image kind code, if present.</param>
		/// <param name="Width">Image width, if present.</param>
		/// <param name="Height">Image height, if present.</param>
		/// <param name="ExtensionData">Raw encoded extension data not interpreted by this parser.</param>
		public ImageInformation2D(ImageDataFormat ImageDataFormat, int? FaceImageKind,
			int? Width, int? Height, byte[][] ExtensionData)
		{
			this.ImageDataFormat = ImageDataFormat;
			this.FaceImageKind = FaceImageKind;
			this.Width = Width;
			this.Height = Height;
			this.ExtensionData = ExtensionData;
		}

		/// <summary>
		/// Image data format.
		/// </summary>
		public ImageDataFormat ImageDataFormat { get; }

		/// <summary>
		/// 2D face image kind code, if present.
		/// </summary>
		public int? FaceImageKind { get; }

		/// <summary>
		/// Image width, if present.
		/// </summary>
		public int? Width { get; }

		/// <summary>
		/// Image height, if present.
		/// </summary>
		public int? Height { get; }

		/// <summary>
		/// Raw encoded extension data not interpreted by this parser.
		/// </summary>
		public byte[][] ExtensionData { get; }
	}
}
