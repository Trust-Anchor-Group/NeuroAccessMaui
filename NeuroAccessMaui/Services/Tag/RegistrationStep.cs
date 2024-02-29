namespace NeuroAccessMaui.Services.Tag
{
	/// <summary>
	/// The different steps of a TAG Profile registration journey.
	/// </summary>
	public enum RegistrationStep
	{
		/// <summary>
		/// Request one of <see cref="PurposeUse" />
		/// </summary>
		RequestPurpose = 0,

		/// <summary>
		/// Validate the phone number 
		/// </summary>
		ValidatePhone = 1,

		/// <summary>
		/// Validate the email address
		/// </summary>
		ValidateEmail = 2,

		/// <summary>
		/// Choose the provider on which to create an account
		/// </summary>
		ChooseProvider = 3,

		/// <summary>
		/// Create a new account
		/// </summary>
		CreateAccount = 4,

		/// <summary>
		/// Create a password
		/// </summary>
		DefinePassword = 5,

		/// <summary>
		/// Profile is completed.
		/// </summary>
		Complete = 6
	}
}
