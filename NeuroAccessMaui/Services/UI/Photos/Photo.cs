namespace NeuroAccessMaui.Services.UI.Photos
{
	/// <summary>
	/// Class containing information about a photo.
	/// </summary>
	/// <param name="Source">Image source</param>
	/// <param name="Rotation">Rotation</param>
	public class Photo(ImageSource Source, int Rotation)
	{
		private readonly ImageSource image = Source;
		private readonly int rotation = Rotation;

		/// <summary>
		/// Class containing information about a photo.
		/// </summary>
		/// <param name="Source">Image source</param>
		public Photo(byte[] Source)
			: this(Source, PhotosLoader.GetImageRotation(Source))
		{
		}

		/// <summary>
		/// Class containing information about a photo.
		/// </summary>
		/// <param name="Source">Image source</param>
		/// <param name="Rotation">Rotation</param>
		public Photo(byte[] Source, int Rotation)
			: this(ImageSource.FromStream(() => new MemoryStream(Source)), Rotation)
		{
		}

		/// <summary>
		/// Image source
		/// </summary>
		public ImageSource Source => this.image;

		/// <summary>
		/// Rotation
		/// </summary>
		public int Rotation => this.rotation;
	}
}
