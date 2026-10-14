namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Describes what a KYC image field captures, so the form and the camera can show matching guidance.
	/// </summary>
	public enum KycCaptureProfile
	{
		/// <summary>
		/// A photo without specific framing, such as a supporting document.
		/// </summary>
		Generic = 0,

		/// <summary>
		/// A photo of the user's face, taken with the front camera.
		/// </summary>
		Selfie = 1,

		/// <summary>
		/// The photo page of a passport.
		/// </summary>
		Passport = 2,

		/// <summary>
		/// The front of an identity card or driver's license.
		/// </summary>
		IdCardFront = 3,

		/// <summary>
		/// The back of an identity card or driver's license.
		/// </summary>
		IdCardBack = 4
	}
}
