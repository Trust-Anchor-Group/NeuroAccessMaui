﻿using NeuroAccessMaui.Services.Storage;
using System.ComponentModel;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Tag
{
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

		/// <summary>
		/// Initial domain.
		/// </summary>
		string? InitialDomain { get; }

		/// <summary>
		/// If the <see cref="InitialDomain"/> employs default XMPP connectivity.
		/// </summary>
		bool InitialDefaultXmppConnectivity { get; }

		/// <summary>
		/// API Key for the <see cref="InitialDomain"/>
		/// </summary>
		string? InitialApiKey { get; }

		/// <summary>
		/// Secret for the <see cref="InitialApiKey"/>
		/// </summary>
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
		/// Selected country. Some countries have the same phone code, so we want to save the selected country
		/// </summary>
		string? SelectedCountry { get; }

		/// <summary>
		/// Verified phone number.
		/// </summary>
		string? PhoneNumber { get; }

		/// <summary>
		/// Verified e-mail address.
		/// </summary>
		string? EMail { get; set; }

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
		string? LegalJid { get; set; }

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
		string? LogJid { get; set; }

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
		/// How the user authenticates itself with the App.
		/// </summary>
		AuthenticationMethod AuthenticationMethod { get; set; }

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
		DateTime? TestOtpTimestamp { get; set; }

		/// <summary>
		/// The legal identity of the current user/profile.
		/// </summary>
		LegalIdentity? LegalIdentity { get; }

		/// <summary>
		/// Any current Identity application.
		/// </summary>
		LegalIdentity? IdentityApplication { get; }

		/// <summary>
		/// Currently selected theme.
		/// </summary>
		AppTheme? Theme { get; }

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
		/// <param name="NewStep">New step</param>
		/// <param name="SupressEvent">If registration step event should be supressed (default=false).</param>
		void GoToStep(RegistrationStep NewStep, bool SupressEvent = false);

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
		/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> has an account but not a legal id,
		/// <c>false</c> otherwise.
		/// </summary>
		/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> has an account but not a legal id,
		/// <c>false</c> otherwise.</returns>
		bool ShouldCreateClient();

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
		/// Sets the phone number used for contacting the user.
		/// </summary>
		/// <param name="Country">Country of the phone number.</param>
		/// <param name="PhoneNumber">Verified phone number.</param>
		void SetPhone(string Country, string PhoneNumber);

		/// <summary>
		/// Set the domain name to connect to.
		/// </summary>
		/// <param name="DomainName">The domain name.</param>
		/// <param name="DefaultXmppConnectivity">If connecting to the domain can be done using default parameters (host=domain, default c2s port).</param>
		/// <param name="Key">Key to use, if an account is to be created.</param>
		/// <param name="Secret">Secret to use, if an account is to be created.</param>
		void SetDomain(string DomainName, bool DefaultXmppConnectivity, string Key, string Secret);

		/// <summary>
		/// Reverses the SetDomain to the Initial* values.
		/// </summary>
		void UndoDomainSelection();

		/// <summary>
		/// Revert the SetDomain
		/// </summary>
		void ClearDomain();

		/// <summary>
		/// Set the account name and password for a <em>new</em> account.
		/// </summary>
		/// <param name="AccountName">The account/user name.</param>
		/// <param name="ClientPasswordHash">The password hash (never send the real password).</param>
		/// <param name="ClientPasswordHashMethod">The hash method used when hashing the password.</param>
		void SetAccount(string AccountName, string ClientPasswordHash, string ClientPasswordHashMethod);

		/// <summary>
		/// Set the account name and password for an <em>existing</em> account.
		/// </summary>
		/// <param name="AccountName">The account/user name.</param>
		/// <param name="ClientPasswordHash">The password hash (never send the real password).</param>
		/// <param name="ClientPasswordHashMethod">The hash method used when hashing the password.</param>
		/// <param name="Identity">The new identity.</param>
		Task SetAccountAndLegalIdentity(string AccountName, string ClientPasswordHash, string ClientPasswordHashMethod, LegalIdentity Identity);

		/// <summary>
		/// Sets the legal identity of the profile.
		/// </summary>
		/// <param name="Identity">Identity to set.</param>
		/// <param name="RemoveOldAttachments">If old attachments should be removed.</param>
		Task SetLegalIdentity(LegalIdentity? Identity, bool RemoveOldAttachments);

		/// <summary>
		/// Sets the legal identity of the profile.
		/// </summary>
		/// <param name="Identity">Identity to set.</param>
		/// <param name="RemoveOldAttachments">If old attachments should be removed.</param>
		Task SetIdentityApplication(LegalIdentity? Identity, bool RemoveOldAttachments);

		/// <summary>
		/// Revert the Set Account
		/// </summary>
		void ClearAccount();

		/// <summary>
		/// Revert the Set LegalIdentity
		/// </summary>
		Task ClearLegalIdentity();

		/// <summary>
		/// Set if the user choose the educational or experimental purpose.
		/// </summary>
		/// <param name="IsTest">If app is in test mode.</param>
		/// <param name="Purpose">Purpose for using the app</param>
		void SetPurpose(bool IsTest, PurposeUse Purpose);

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the revoked identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="revokedIdentity">The revoked identity to use.</param>
		Task RevokeLegalIdentity(LegalIdentity revokedIdentity);

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the compromised identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="compromisedIdentity">The compromised identity to use.</param>
		Task CompromiseLegalIdentity(LegalIdentity compromisedIdentity);

		/// <summary>
		/// Used during XMPP service discovery. Sets the file upload parameters.
		/// </summary>
		/// <param name="httpFileUploadJid">The http file upload id.</param>
		/// <param name="maxSize">The max size allowed.</param>
		void SetFileUploadParameters(string httpFileUploadJid, long maxSize);

		/// <summary>
		/// Computes a hash of the specified PIN.
		/// </summary>
		/// <param name="Pin">The PIN whose hash to compute.</param>
		/// <returns>Hash Digest</returns>
		string ComputePinHash(string Pin);

		/// <summary>
		/// Clears the entire profile.
		/// </summary>
		void ClearAll();

		/// <summary>
		/// Validates if the <paramref name="Pin"/> is strong enough.
		/// </summary>
		/// <param name="Pin">PIN to validate.</param>
		/// <returns>A <see cref="PinStrength"/> value indicating if the <paramref name="Pin"/> is strong enough.</returns>
		PinStrength ValidatePinStrength(string? Pin);

		/// <summary>
		/// Sets the preferred theme.
		/// </summary>
		/// <param name="Theme">Theme</param>
		void SetTheme(AppTheme Theme);

		/// <summary>
		/// Sets the preferred theme.
		/// </summary>
		void SetTheme();
	}
}
