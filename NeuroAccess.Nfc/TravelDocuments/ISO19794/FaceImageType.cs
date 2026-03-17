namespace NeuroAccess.Nfc.TravelDocuments.ISO19794
{
	/// <summary>
	/// ICAO 9303 / ISO 19794-5 face image type.
	/// </summary>
	public enum FaceImageType
	{
		/// <summary>
		/// Basic or unspecified
		/// </summary>
		Basic = 0,

		/// <summary>
		/// Token face image (ICAO “portrait” style, cropped, neutral expression)
		/// </summary>
		Portrait = 1,

		/// <summary>
		/// Full frontal face image (uncropped, entire head visible)
		/// </summary>
		FullFrontal = 2,

		/// <summary>
		/// Other (profile, non‑frontal, etc.)
		/// </summary>
		Other = 3
	}
}
