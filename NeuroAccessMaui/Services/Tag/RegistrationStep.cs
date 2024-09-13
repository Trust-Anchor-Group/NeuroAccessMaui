namespace NeuroAccessMaui.Services.Tag
{
	/// <summary>
	/// The different steps of a TAG Profile registration journey.
	/// </summary>
	public enum RegistrationStep
	{

		GetStarted = 10,

		/// <summary>
		/// Validate the phone number 
		/// </summary>
		ValidatePhone = 20,

		/// <summary>
		/// Validate the email address
		/// </summary>
		ValidateEmail = 30,

		/// <summary>
		/// Choose the provider on which to create an account
		/// </summary>
		ChooseProvider = 40,

		/// <summary>
		/// Create a new account
		/// </summary>
		CreateAccount = 50,

		/// <summary>
		/// Create a password
		/// </summary>
		DefinePassword = 60,

		/// <summary>
		/// Profile is completed.
		/// </summary>
		Complete = 70,

		/// <summary>
		/// Enable biometric authentication
		/// </summary>
		Biometrics = 80,

		/// <summary>
		/// Finalize profile creation
		/// </summary>
		Finalize = 90
	}
}
