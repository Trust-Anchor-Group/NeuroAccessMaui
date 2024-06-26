﻿namespace NeuroAccessMaui.Services.Tag
{
	/// <summary>
	/// The different steps of a TAG Profile registration journey.
	/// </summary>
	public enum RegistrationStep
	{

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
		Complete = 6,

		/// <summary>
		/// Enable biometric authentication
		/// </summary>
		Biometrics = 7,

		/// <summary>
		/// Finalize profile creation
		/// </summary>
		Finalize = 8
	}
}
