namespace NeuroAccessMaui.Services.Tag
{
	/// <summary>
	/// How the user authenticates itself with the App.
	/// </summary>
	public enum AuthenticationMethod
	{
		/// <summary>
		/// The password defined during onboarding.
		/// </summary>
		Password,

		/// <summary>
		/// The device's Fingerprint sensor.
		/// </summary>
		Fingerprint
	}
}
