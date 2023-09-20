namespace NeuroAccessMaui.Services.Tag;

/// <summary>
/// Represents a result of validating PIN strength.
/// </summary>
public enum PinStrength
{
	/// <summary>
	/// A PIN is strong enough.
	/// </summary>
	Strong,

	/// <summary>
	/// A PIN is <c>null</c> or contains not enough symbols from all character classes (digits, letters or signs).
	/// </summary>
	NotEnoughDigitsLettersSigns,

	/// <summary>
	/// A PIN contains enough digits but not enough letters or signs.
	/// </summary>
	NotEnoughLettersOrSigns,

	/// <summary>
	/// A PIN contains enough signs but not enough letters or digits.
	/// </summary>
	NotEnoughLettersOrDigits,

	/// <summary>
	/// A PIN contains enough letters but not enough digits or signs.
	/// </summary>
	NotEnoughDigitsOrSigns,

	/// <summary>
	/// A PIN is too short.
	/// <para>
	/// <see cref="TooShort"/> corresponds to a PIN which has enough digits, letters or signs to satisfy the variety rule
	/// but is still too short to be acceptable.
	/// </para>
	/// </summary>
	TooShort,

	/// <summary>
	/// A PIN contains too many identical symbols.
	/// </summary>
	TooManyIdenticalSymbols,

	/// <summary>
	/// A PIN contains too many sequenced symbols.
	/// </summary>
	TooManySequencedSymbols,

	/// <summary>
	/// A PIN contains the legal identity personal number.
	/// </summary>
	ContainsPersonalNumber,

	/// <summary>
	/// A PIN contains the legal identity phone number.
	/// </summary>
	ContainsPhoneNumber,

	/// <summary>
	/// A PIN contains the legal identity e-mail.
	/// </summary>
	ContainsEMail,

	/// <summary>
	/// A PIN contains a word from the legal identity name.
	/// </summary>
	ContainsName,

	/// <summary>
	/// A PIN contains a word from the legal identity address.
	/// </summary>
	ContainsAddress,
}
