using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;

// !!! keep the namespace as is. It's important for the database
namespace NeuroAccessMaui.Services
{
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
		public string? ObjectId { get; set; }

		[DefaultValue(false)]
		public bool InitialDefaultXmppConnectivity { get; set; }

		[DefaultValueNull]
		public string? InitialDomain { get; set; }

		[DefaultValueNull]
		public string? InitialApiKey { get; set; }

		[DefaultValueNull]
		public string? InitialApiSecret { get; set; }

		/// <summary>
		/// Current domain
		/// </summary>
		[DefaultValueNull]
		public string? Domain { get; set; }

		/// <summary>
		/// API Key
		/// </summary>
		[DefaultValueNull]
		public string? ApiKey { get; set; }

		/// <summary>
		/// API Secret
		/// </summary>
		[DefaultValueNull]
		public string? ApiSecret { get; set; }

		/// <summary>
		/// Selected Country
		/// </summary>
		[DefaultValueNull]
		public string? SelectedCountry { get; set; }

		/// <summary>
		/// Verified Phone Number
		/// </summary>
		[DefaultValueNull]
		public string? PhoneNumber { get; set; }

		/// <summary>
		/// Verified e-mail address
		/// </summary>
		[DefaultValueNull]
		public string? EMail { get; set; }

		/// <summary>
		/// If connecting to the domain can be done using default parameters (host=domain, default c2s port).
		/// </summary>
		[DefaultValue(false)]
		public bool DefaultXmppConnectivity { get; set; }

		/// <summary>
		/// Account name
		/// </summary>
		[DefaultValueNull]
		public string? Account { get; set; }

		/// <summary>
		/// Password hash
		/// </summary>
		[DefaultValueNull]
		public string? PasswordHash { get; set; }

		/// <summary>
		/// Password hash method
		/// </summary>
		[DefaultValueNull]
		public string? PasswordHashMethod { get; set; }

		/// <summary>
		/// Legal Jabber Id
		/// </summary>
		[DefaultValueNull]
		public string? LegalJid { get; set; }

		/// <summary>
		/// Http File Upload Jabber Id
		/// </summary>
		[DefaultValueNull]
		public string? HttpFileUploadJid { get; set; }

		/// <summary>
		/// Http File Upload max file size
		/// </summary>
		[DefaultValue(0)]
		public long HttpFileUploadMaxSize { get; set; }

		/// <summary>
		/// Log Jabber JID
		/// </summary>
		[DefaultValueNull]
		public string? LogJid { get; set; }

		/// <summary>
		/// The hash of the user's pin.
		/// </summary>
		[DefaultValueNull]
		public string? PinHash { get; set; }

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
		[DefaultValue(Tag.PurposeUse.Personal)]
		public Tag.PurposeUse Purpose { get; set; }

		/// <summary>
		/// Set to current timestamp if the user used a Test OTP Code.
		/// </summary>
		[DefaultValueNull]
		public DateTime? TestOtpTimestamp { get; set; }

		/// <summary>
		/// User's current legal identity.
		/// </summary>
		[DefaultValueNull]
		public LegalIdentity? LegalIdentity { get; set; }

		/// <summary>
		/// Current step in the registration process.
		/// </summary>
		[DefaultValue(Tag.RegistrationStep.RequestPurpose)]
		public Tag.RegistrationStep Step { get; set; }

		/// <summary>
		/// Currently selected theme.
		/// </summary>
		[DefaultValueNull]
		public AppTheme? Theme { get; set; }
	}
}
