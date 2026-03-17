namespace NeuroAccess.Nfc.TravelDocuments.ISO19794
{
	/// <summary>
	/// Contains a representation of biometric data of a face.
	/// </summary>
	public class RepresentationFace : Representation
	{
		/// <summary>
		/// Contains a representation of biometric data of a face.
		/// </summary>
		/// <param name="FaceImageType">Face image type</param>
		/// <param name="ImageDataType">Image data type</param>
		/// <param name="Width">Image width</param>
		/// <param name="Height">Image height</param>
		/// <param name="ImageData">Raw Image data</param>
		/// <param name="Gender">Gender</param>
		/// <param name="EyeColour">Eye colour</param>
		/// <param name="HairColour">Hair colour</param>
		public RepresentationFace(FaceImageType FaceImageType, ImageDataType ImageDataType,
			int Width, int Height, byte[] ImageData, Gender Gender, EyeColour EyeColour,
			HairColour HairColour)
			: base(ImageDataType, Width, Height, ImageData)
		{
			this.FaceImageType = FaceImageType;
			this.Gender = Gender;
			this.EyeColour = EyeColour;
			this.HairColour = HairColour;
		}

		/// <summary>
		/// Face image type.
		/// </summary>
		public FaceImageType FaceImageType { get; }

		/// <summary>
		/// Gender
		/// </summary>
		public Gender Gender { get; }

		/// <summary>
		/// Eye colour
		/// </summary>
		public EyeColour EyeColour { get; }

		/// <summary>
		/// Hair colour
		/// </summary>
		public HairColour HairColour { get; }
	}
}
