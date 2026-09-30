namespace DevStorybook.Stories
{
	/// <summary>
	/// Identifies a deterministic onboarding story state.
	/// </summary>
	public enum PhoneVerificationStoryState
	{
		/// <summary>The untouched screen state.</summary>
		Default,
		/// <summary>The explicitly empty screen state.</summary>
		Empty,
		/// <summary>A valid phone number.</summary>
		ValidPhoneNumber,
		/// <summary>An invalid phone number.</summary>
		InvalidPhoneNumber,
		/// <summary>A request that remains in progress.</summary>
		SendingCode,
		/// <summary>A successful send result.</summary>
		CodeSent,
		/// <summary>A deterministic backend failure.</summary>
		BackendError,
		/// <summary>A deterministic network failure.</summary>
		NetworkError,
		/// <summary>The United States country picker state.</summary>
		CountryUnitedStates,
		/// <summary>The Sweden country picker state.</summary>
		CountrySweden,
		/// <summary>The disabled country picker state.</summary>
		CountryDisabled,
		/// <summary>An empty phone input component.</summary>
		PhoneInputEmpty,
		/// <summary>A valid phone input component.</summary>
		PhoneInputValid,
		/// <summary>An invalid phone input component.</summary>
		PhoneInputInvalid,
		/// <summary>A disabled phone input component.</summary>
		PhoneInputDisabled,
		/// <summary>A focused phone input component.</summary>
		PhoneInputFocused,
		/// <summary>The ID-provider selection screen.</summary>
		IdentityProvider,
		/// <summary>An empty e-mail verification screen.</summary>
		EmailEmpty,
		/// <summary>A valid e-mail verification screen.</summary>
		EmailValid,
		/// <summary>An invalid e-mail verification screen.</summary>
		EmailInvalid,
		/// <summary>An empty username screen.</summary>
		UsernameEmpty,
		/// <summary>A filled, valid username screen.</summary>
		UsernameValid,
		/// <summary>An invalid username with a suggested alternative.</summary>
		UsernameInvalid,
		/// <summary>The account and identity creation progress screen.</summary>
		AccountCreation,
		/// <summary>An empty PIN creation screen.</summary>
		PinEmpty,
		/// <summary>A matching PIN creation screen.</summary>
		PinValid,
		/// <summary>A PIN confirmation mismatch.</summary>
		PinMismatch,
		/// <summary>The biometric enrollment screen.</summary>
		Biometrics,
		/// <summary>The completed onboarding screen.</summary>
		Success,
		/// <summary>The default phone-code screen.</summary>
		PhoneCodeDefault,
		/// <summary>An empty phone-code screen.</summary>
		PhoneCodeEmpty,
		/// <summary>An incomplete phone code.</summary>
		PhoneCodeInvalid,
		/// <summary>A phone code being verified.</summary>
		PhoneCodeVerifying,
		/// <summary>A verified phone code.</summary>
		PhoneCodeVerified,
		/// <summary>An expired phone code.</summary>
		PhoneCodeExpired,
		/// <summary>A phone-code backend failure.</summary>
		PhoneCodeBackendError,
		/// <summary>The default e-mail-code screen.</summary>
		EmailCodeDefault,
		/// <summary>An empty e-mail-code screen.</summary>
		EmailCodeEmpty,
		/// <summary>An incomplete e-mail code.</summary>
		EmailCodeInvalid,
		/// <summary>An e-mail code being verified.</summary>
		EmailCodeVerifying,
		/// <summary>A verified e-mail code.</summary>
		EmailCodeVerified,
		/// <summary>An expired e-mail code.</summary>
		EmailCodeExpired,
		/// <summary>An e-mail-code backend failure.</summary>
		EmailCodeBackendError
	}
}
