using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.UI.Photos
{
	/// <summary>
	/// Class containing information about a photo.
	/// </summary>
	/// <param name="Binary">Binary representation of photo.</param>
	/// <param name="Rotation">Rotation</param>
	public class Photo(byte[] Binary, int Rotation, Attachment? Attachment)
	{
		/// <summary>
		/// Class containing information about a photo.
		/// </summary>
		/// <param name="Source">Image source</param>
		public Photo(byte[] Source)
			: this(Source, PhotosLoader.GetImageRotation(Source), null)
		{
		}

		/// <summary>
		/// Image source
		/// </summary>
		public ImageSource Source { get; } = ImageSource.FromStream(() => new MemoryStream(Binary));

		/// <summary>
		/// Binary representation of image.
		/// </summary>
		public byte[] Binary { get; } = Binary;

		/// <summary>
		/// Rotation
		/// </summary>
		public int Rotation { get; } = Rotation;

		/// <summary>
		/// Attachment
		/// </summary>
		public Attachment? Attachment { get; } = Attachment;
	}
}
