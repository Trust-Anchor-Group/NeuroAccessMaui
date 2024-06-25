namespace NeuroAccessMaui.Services
{
	/// <summary>
	///	Enum representing the device biometric method for authentication.
	/// </summary>
	public enum BiometricMethod
	{
		/// <summary>
		///  The device supports face recognition.
		/// </summary>
		Face = 0,
		/// <summary>
		/// The device supports fingerprint recognition.
		/// </summary>
		Fingerprint = 1,
		/// <summary>
		/// The device supports face recognition, IOS specific.
		/// </summary>
		FaceId = 50,
		/// <summary>
		/// device supports fingerprint recognition, IOS specific.
		/// </summary>
		TouchId = 51,
		/// <summary>
		/// The device supports biometric authentication, but the method is unknown.
		/// </summary>
		Unknown = 100,
		/// <summary>
		/// The device does not support biometric authentication.
		/// </summary>
		None = 101
	}
}
