namespace NeuroAccess.Nfc.TravelDocuments.ISO19794
{
	/// <summary>
	/// Contains a representation of biometric data.
	/// </summary>
	public abstract class Representation
	{
		/// <summary>
		/// Contains a representation of biometric data.
		/// </summary>
		/// <param name="ImageDataType">Image data type</param>
		/// <param name="Width">Image width</param>
		/// <param name="Height">Image height</param>
		/// <param name="ImageData">Raw Image data</param>
		public Representation(ImageDataType ImageDataType, int Width, int Height,
			byte[] ImageData)
		{
			this.ImageDataType = ImageDataType;
			this.Width = Width;
			this.Height = Height;
			this.ImageData = ImageData;
		}

		/// <summary>
		/// Image data type.
		/// </summary>
		public ImageDataType ImageDataType { get; }

		/// <summary>
		/// Width of image, in pixels.
		/// </summary>
		public int Width { get; }

		/// <summary>
		/// Height of image, in pixels.
		/// </summary>
		public int Height { get; }

		/// <summary>
		/// Raw binary image data.
		/// </summary>
		public byte[] ImageData { get; }
	}
}
