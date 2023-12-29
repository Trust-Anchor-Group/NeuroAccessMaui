namespace NeuroAccessMaui.Services.Tag
{
	/// <summary>
	/// How the user authenticates itself with the App.
	/// </summary>
	public enum AuthenticationMethod
	{
		/// <summary>
		/// The PIN code defined during onboarding.
		/// </summary>
		Pin,

		/// <summary>
		/// The device's Fingerprint sensor.
		/// </summary>
		Fingerprint
	}
}
