using NeuroAccessMaui.Services.Storage;
using System.ComponentModel;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Tag;

/// <summary>
/// The TAG Profile is the heart of the digital identity for a specific user/device.
/// Use this instance to add and make a profile more complete.
/// The TAG Profile holds relevant data connected to not only where the user is in the registration process,
/// but also Xmpp identifiers.
/// </summary>
[DefaultImplementation(typeof(TagProfile))]
public interface ITagProfile
{
	/// <summary>
	/// An event that triggers during the registration/profile build process, as the profile becomes more/less complete.
	/// </summary>
	event EventHandler? StepChanged;

	/// <summary>
	/// An event that fires whenever any property on the <see cref="ITagProfile"/> changes.
	/// </summary>
	event PropertyChangedEventHandler? Changed;

	string? InitialDomain { get; }
	bool InitialDefaultXmppConnectivity { get; }
	string? InitialApiKey { get; }
	string? InitialApiSecret { get; }

	/// <summary>
	/// The domain this profile is connected to.
	/// </summary>
	string? Domain { get; }

	/// <summary>
	/// If connecting to the domain can be done using default parameters (host=domain, default c2s port).
	/// </summary>
	bool DefaultXmppConnectivity { get; }

	/// <summary>
	/// API Key, for creating new account.
	/// </summary>
	string? ApiKey { get; }

	/// <summary>
	/// API Secret, for creating new account.
	/// </summary>
	string? ApiSecret { get; }

	/// <summary>
	/// Verified phone number.
	/// </summary>
	string? PhoneNumber { get; }

	/// <summary>
	/// Verified e-mail address.
	/// </summary>
	string? EMail { get; }

	/// <summary>
	/// The account name for this profile
	/// </summary>
	string? Account { get; }

	/// <summary>
	/// A hash of the current password.
	/// </summary>
	string? PasswordHash { get; }

	/// <summary>
	/// The hash method used for hashing the password.
	/// </summary>
	string? PasswordHashMethod { get; }

	/// <summary>
	/// The Jabber Legal JID for this user/profile.
	/// </summary>
	string? LegalJid { get; }

	/// <summary>
	/// The XMPP server's file upload Jid.
	/// </summary>
	string? HttpFileUploadJid { get; }

	/// <summary>
	/// The XMPP server's max size for file uploads.
	/// </summary>
	long HttpFileUploadMaxSize { get; }

	/// <summary>
	/// The XMPP server's log Jid.
	/// </summary>
	string? LogJid { get; }

	/// <summary>
	/// This profile's current registration step.
	/// </summary>
	RegistrationStep Step { get; }

	/// <summary>
	/// Returns <c>true</c> if file upload is supported for the specified XMPP server, <c>false</c> otherwise.
	/// </summary>
	bool FileUploadIsSupported { get; }

	/// <summary>
	/// The user's PIN value.
	/// </summary>
	string Pin { set; }

	/// <summary>
	/// A hashed version of the user's <see cref="Pin"/>.
	/// </summary>
	string? PinHash { get; }

	/// <summary>
	/// Indicates if the user has a <see cref="Pin"/>.
	/// </summary>
	bool HasPin { get; }

	/// <summary>
	/// Returns <c>true</c> if the user choose the educational or experimental purpose.
	/// </summary>
	bool IsTest { get; }

	/// <summary>
	/// Purpose for using the app.
	/// </summary>
	PurposeUse Purpose { get; }

	/// <summary>
	/// Returns a timestamp if the user used a Test OTP Code.
	/// </summary>
	DateTime? TestOtpTimestamp { get; }

	/// <summary>
	/// The legal identity of the curren user/profile.
	/// </summary>
	LegalIdentity? LegalIdentity { get; }

	/// <summary>
	/// Returns <c>true</c> if the current <see cref="ITagProfile"/> has changed values and need saving, <c>false</c> otherwise.
	/// </summary>
	bool IsDirty { get; }

	/// <summary>
	/// Converts the current <see cref="ITagProfile"/> to a <see cref="TagConfiguration"/> object that can be persisted to the <see cref="IStorageService"/>.
	/// </summary>
	/// <returns>Configuration object</returns>
	TagConfiguration ToConfiguration();

	/// <summary>
	/// Copies values from the <see cref="TagConfiguration"/> to this instance.
	/// </summary>
	/// <param name="configuration"></param>
	void FromConfiguration(TagConfiguration configuration);

	/// <summary>
	/// Changes the current onboarding step.
	/// </summary>
	void GoToStep(RegistrationStep NewStep);

	/// <summary>
	/// Returns <c>true</c> if the current <see cref="ITagProfile"/> needs to have its values updated, <c>false</c> otherwise.
	/// </summary>
	/// <returns>If values need updating</returns>
	bool NeedsUpdating();

	/// <summary>
	/// Returns <c>true</c> if the current <see cref="ITagProfile"/> needs to have its legal identity updated, <c>false</c> otherwise.
	/// </summary>
	/// <returns>If legal identity need updating</returns>
	bool LegalIdentityNeedsUpdating();

	/// <summary>
	/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete or is just awaiting validation, <c>false</c> otherwise.
	/// </summary>
	/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete or is just awaiting validation, <c>false</c> otherwise.</returns>
	bool IsCompleteOrWaitingForValidation();

	/// <summary>
	/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete, <c>false</c> otherwise.
	/// </summary>
	/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete, <c>false</c> otherwise.</returns>
	bool IsComplete();

	/// <summary>
	/// Resets the <see cref="IsDirty"/> flag, can be used after persisting values to <see cref="IStorageService"/>.
	/// </summary>
	void ResetIsDirty();

	/// <summary>
	/// Step 1 - sets the phone number used for contacting the user.
	/// </summary>
	/// <param name="PhoneNumber">Verified phone number.</param>
	void SetPhone(string PhoneNumber);

	/// <summary>
	/// Step 1 - sets the e-mail address used for contacting the user.
	/// </summary>
	/// <param name="EMail">Verified e-mail address.</param>
	void SetEMail(string EMail);

	/// <summary>
	/// Step 1 - set the domain name to connect to.
	/// </summary>
	/// <param name="domainName">The domain name.</param>
	/// <param name="defaultXmppConnectivity">If connecting to the domain can be done using default parameters (host=domain, default c2s port).</param>
	/// <param name="Key">Key to use, if an account is to be created.</param>
	/// <param name="Secret">Secret to use, if an account is to be created.</param>
	void SetDomain(string domainName, bool defaultXmppConnectivity, string Key, string Secret);

	/// <summary>
	/// Reverses the SetDomain to the Initial* values.
	/// </summary>
	void UndoDomainSelection();

	/// <summary>
	/// Revert Step 1.
	/// </summary>
	void ClearDomain();

	/// <summary>
	/// An alternative Step 1, used for accounts with an obsoleted identity, - validate contact info (the same or updated) without changing account data.
	/// </summary>
	void RevalidateContactInfo();

	/// <summary>
	/// Revert an alternative Step 1, used for accounts with an obsoleted identity, - invalidate contact info without erasing the legal identity or otherwise changing the account.
	/// </summary>
	void InvalidateContactInfo();

	/// <summary>
	/// Step 2 - set the account name and password for a <em>new</em> account.
	/// </summary>
	/// <param name="accountName">The account/user name.</param>
	/// <param name="clientPasswordHash">The password hash (never send the real password).</param>
	/// <param name="clientPasswordHashMethod">The hash method used when hashing the password.</param>
	void SetAccount(string accountName, string clientPasswordHash, string clientPasswordHashMethod);

	/// <summary>
	/// Step 2 and 3 - set the account name and password for an <em>existing</em> account.
	/// </summary>
	/// <param name="accountName">The account/user name.</param>
	/// <param name="clientPasswordHash">The password hash (never send the real password).</param>
	/// <param name="clientPasswordHashMethod">The hash method used when hashing the password.</param>
	/// <param name="identity">The new identity.</param>
	void SetAccountAndLegalIdentity(string accountName, string clientPasswordHash, string clientPasswordHashMethod, LegalIdentity identity);

	/// <summary>
	/// Revert Step 2.
	/// </summary>
	void ClearAccount(bool GoToPrevStep = true);

	/// <summary>
	/// Step 3 - set the legal identity of a newly created account.
	/// </summary>
	/// <param name="legalIdentity">The legal identity to use.</param>
	void SetLegalIdentity(LegalIdentity legalIdentity);

	/// <summary>
	/// Revert Step 3.
	/// </summary>
	void ClearLegalIdentity();

	/// <summary>
	/// Step 4 - set the current legal identity as validated.
	/// </summary>
	void SetIsValidated();

	/// <summary>
	/// Revert Step 4.
	/// </summary>
	void ClearIsValidated();

	/// <summary>
	///  Step 5 - Set a pin to use for protecting the account.
	/// </summary>
	/// <param name="Pin">The pin to use.</param>
	/// <param name="AddOrUpdatePin">
	/// If we should use <paramref name="Pin"/> to set or clear pin or we should ignore <paramref name="Pin"/> and just complete th step.
	/// </param>
	void CompletePinStep(string Pin, bool AddOrUpdatePin = true);

	/// <summary>
	/// Revert Step 5.
	/// </summary>
	void RevertPinStep();

	/// <summary>
	/// Step 1 - Set if the user choose the educational or experimental purpose.
	/// </summary>
	/// <param name="IsTest">If app is in test mode.</param>
	/// <param name="Purpose">Purpose for using the app</param>
	void SetPurpose(bool IsTest, PurposeUse Purpose);

	/// <summary>
	/// Step 1 - Set if the user used a Test OTP Code.
	/// </summary>
	void SetTestOtpTimestamp(DateTime? timestamp);

	/// <summary>
	/// Sets the current <see cref="LegalIdentity"/> to the revoked identity, and reverses the <see cref="Step"/> property.
	/// </summary>
	/// <param name="revokedIdentity">The revoked identity to use.</param>
	void RevokeLegalIdentity(LegalIdentity revokedIdentity);

	/// <summary>
	/// Sets the current <see cref="LegalIdentity"/> to the compromised identity, and reverses the <see cref="Step"/> property.
	/// </summary>
	/// <param name="compromisedIdentity">The compromised identity to use.</param>
	void CompromiseLegalIdentity(LegalIdentity compromisedIdentity);

	/// <summary>
	/// Used during XMPP service discovery. Sets the legal id.
	/// </summary>
	/// <param name="legalJid">The legal id.</param>
	void SetLegalJid(string legalJid);

	/// <summary>
	/// Used during XMPP service discovery. Sets the file upload parameters.
	/// </summary>
	/// <param name="httpFileUploadJid">The http file upload id.</param>
	/// <param name="maxSize">The max size allowed.</param>
	void SetFileUploadParameters(string httpFileUploadJid, long maxSize);

	/// <summary>
	/// Used during XMPP service discovery. Sets the log id.
	/// </summary>
	/// <param name="logJid">The log id.</param>
	void SetLogJid(string logJid);

	/// <summary>
	/// Computes a hash of the specified PIN.
	/// </summary>
	/// <param name="pin">The PIN whose hash to compute.</param>
	/// <returns>Hash Digest</returns>
	string ComputePinHash(string pin);

	/// <summary>
	/// Clears the entire profile.
	/// </summary>
	void ClearAll();

	/// <summary>
	/// Validates if the <paramref name="Pin"/> is strong enough.
	/// </summary>
	/// <param name="Pin">PIN to validate.</param>
	/// <returns>A <see cref="PinStrength"/> value indicating if the <paramref name="Pin"/> is strong enough.</returns>
	PinStrength ValidatePinStrength(string Pin);
}
