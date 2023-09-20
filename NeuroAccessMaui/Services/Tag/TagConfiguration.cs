using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;

// !!! keep the namespace as is. It's impotant for the database
namespace NeuroAccessMaui.Services;

/// <summary>
/// A simple POCO object for serializing and deserializing configuration properties.
/// </summary>
[CollectionName("Configuration")]
public sealed class TagConfiguration
{
	/// <summary>
	/// The primary key in persistent storage.
	/// </summary>
	[ObjectId]
	public string ObjectId { get; set; }

	/// <summary>
	/// Current domain
	/// </summary>
	[DefaultValueStringEmpty]
	public string Domain { get; set; }

	/// <summary>
	/// API Key
	/// </summary>
	[DefaultValueStringEmpty]
	public string ApiKey { get; set; }

	/// <summary>
	/// API Secret
	/// </summary>
	[DefaultValueStringEmpty]
	public string ApiSecret { get; set; }

	/// <summary>
	/// Verified Phone Number
	/// </summary>
	[DefaultValueStringEmpty]
	public string PhoneNumber { get; set; }

	/// <summary>
	/// Verified e-mail address
	/// </summary>
	[DefaultValueStringEmpty]
	public string EMail { get; set; }

	/// <summary>
	/// If connecting to the domain can be done using default parameters (host=domain, default c2s port).
	/// </summary>
	[DefaultValue(false)]
	public bool DefaultXmppConnectivity { get; set; }

	/// <summary>
	/// Account name
	/// </summary>
	[DefaultValueStringEmpty]
	public string Account { get; set; }

	/// <summary>
	/// Password hash
	/// </summary>
	[DefaultValueStringEmpty]
	public string PasswordHash { get; set; }

	/// <summary>
	/// Password hash method
	/// </summary>
	[DefaultValueStringEmpty]
	public string PasswordHashMethod { get; set; }

	/// <summary>
	/// Legal Jabber Id
	/// </summary>
	[DefaultValueStringEmpty]
	public string LegalJid { get; set; }

	/// <summary>
	/// Http File Upload Jabber Id
	/// </summary>
	[DefaultValueStringEmpty]
	public string HttpFileUploadJid { get; set; }

	/// <summary>
	/// Http File Upload max file size
	/// </summary>
	[DefaultValueNull]
	public long? HttpFileUploadMaxSize { get; set; }

	/// <summary>
	/// Log Jabber JID
	/// </summary>
	[DefaultValueStringEmpty]
	public string LogJid { get; set; }

	/// <summary>
	/// The hash of the user's pin.
	/// </summary>
	[DefaultValueStringEmpty]
	public string PinHash { get; set; }

	/// <summary>
	/// Set to true if the PIN should be used.
	/// </summary>
	[DefaultValue(false)]
	public bool UsePin { get; set; }

	/// <summary>
	/// Set to true if the user choose the educational or experimental purpose.
	/// </summary>
	[DefaultValue(false)]
	public bool IsTest { get; set; }

	/// <summary>
	/// Purpose for using the app
	/// </summary>
	[DefaultValue(PurposeUse.Personal)]
	public PurposeUse Purpose { get; set; }

	/// <summary>
	/// Set to current timestamp if the user used a Test OTP Code.
	/// </summary>
	[DefaultValueNull]
	public DateTime? TestOtpTimestamp { get; set; }

	/// <summary>
	/// User's current legal identity.
	/// </summary>
	[DefaultValueNull]
	public LegalIdentity LegalIdentity { get; set; }

	/// <summary>
	/// Current step in the registration process.
	/// </summary>
	[DefaultValue(Tag.RegistrationStep.ValidateContactInfo)]
	public Tag.RegistrationStep Step { get; set; }
}
