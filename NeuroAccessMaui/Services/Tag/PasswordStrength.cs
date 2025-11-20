namespace NeuroAccessMaui.Services.Tag
{
	/// <summary>
	/// Represents a result of validating password strength.
	/// </summary>
	public enum PasswordStrength
	{
		/// <summary>
		/// A password is strong enough.
		/// </summary>
		Strong,

		/// <summary>
		/// A password is <c>null</c> or contains not enough symbols from all character classes (digits, letters or signs).
		/// </summary>
		NotEnoughDigitsLettersSigns,

		/// <summary>
		/// A password contains enough digits but not enough letters or signs.
		/// </summary>
		NotEnoughLettersOrSigns,

		/// <summary>
		/// A password contains enough signs but not enough letters or digits.
		/// </summary>
		NotEnoughLettersOrDigits,

		/// <summary>
		/// A password contains enough letters but not enough digits or signs.
		/// </summary>
		NotEnoughDigitsOrSigns,

		/// <summary>
		/// A password is too short.
		/// <para>
		/// <see cref="TooShort"/> corresponds to a password which has enough digits, letters or signs to satisfy the variety rule
		/// but is still too short to be acceptable.
		/// </para>
		/// </summary>
		TooShort,

		/// <summary>
		/// A password contains too many identical symbols (global frequency of a symbol exceeds limit).
		/// </summary>
		TooManyIdenticalSymbols,

		/// <summary>
		/// A password contains too many sequenced symbols.
		/// </summary>
		TooManySequencedSymbols,

		/// <summary>
		/// A password contains a consecutive run of the same symbol that is too long (e.g. 111111).
		/// </summary>
		TooManyRepeatingSymbols,

		/// <summary>
		/// A password contains the legal identity personal number.
		/// </summary>
		ContainsPersonalNumber,

		/// <summary>
		/// A password contains the legal identity phone number.
		/// </summary>
		ContainsPhoneNumber,

		/// <summary>
		/// A password contains the legal identity e-mail.
		/// </summary>
		ContainsEMail,

		/// <summary>
		/// A password contains a word from the legal identity name.
		/// </summary>
		ContainsName,

		/// <summary>
		/// A password contains a word from the legal identity address.
		/// </summary>
		ContainsAddress,
	}
}
