using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Tag;
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

		/// <summary>
		/// If the initially selected domain uses default XMPP connectivity
		/// </summary>
		[DefaultValue(false)]
		public bool InitialDefaultXmppConnectivity { get; set; }

		/// <summary>
		/// Initially selected domain
		/// </summary>
		[DefaultValueNull]
		public string? InitialDomain { get; set; }

		/// <summary>
		/// Initially selected API Key
		/// </summary>
		[DefaultValueNull]
		public string? InitialApiKey { get; set; }

		/// <summary>
		/// Initially selected API Secret
		/// </summary>
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
		/// User's first name(s).
		/// </summary>
		[DefaultValueNull]
		public string? FirstName { get; set; }

		/// <summary>
		/// User's last name(s).
		/// </summary>
		[DefaultValueNull]
		public string? LastName { get; set; }

		/// <summary>
		/// User's friendly name.
		/// </summary>
		[DefaultValueNull]
		public string? FriendlyName { get; set; }

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
		/// Password needs update
		/// </summary>
		[DefaultValue(false)]
		public bool? PasswordNeedsUpdate { get; set; }

		/// <summary>
		/// Is the password numeric?
		/// </summary>
		[DefaultValueNull]
		public bool IsNumericPassword { get; set; }

		/// <summary>
		/// Legal Jabber Id
		/// </summary>
		[DefaultValueNull]
		public string? LegalJid { get; set; }

		/// <summary>
		/// The Thing Registry JID
		/// </summary>
		[DefaultValueStringEmpty]
		public string? RegistryJid { get; set; }

		/// <summary>
		/// Provisioning Jabber Id
		/// </summary>
		[DefaultValueStringEmpty]
		public string? ProvisioningJid { get; set; }

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
		/// eDaler Service JID
		/// </summary>
		[DefaultValueStringEmpty]
		public string? EDalerJid { get; set; }

		/// <summary>
		/// Neuro-Features Service JID
		/// </summary>
		[DefaultValueStringEmpty]
		public string? NeuroFeaturesJid { get; set; }

		/// <summary>
		/// PubSub Service JID
		/// </summary>
		[DefaultValueStringEmpty]
		public string? PubSubJid { get; set; }

		/// <summary>
		/// Trust Provider Id
		/// </summary>
		[DefaultValueStringEmpty]
		public string? TrustProviderId { get; set; }

		/// <summary>
		/// If Push Notification is supported by server.
		/// </summary>
		[DefaultValue(false)]
		public bool SupportsPushNotification { get; set; }

		/// <summary>
		/// The hash of the user's pin.
		/// </summary>
		[DefaultValueNull]
		public string? PinHash { get; set; }

		/// <summary>
		/// Set to true if the password should be used.
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
#if DEBUG
		[DefaultValue(PurposeUse.Experimental)]
		public PurposeUse Purpose { get; set; } = PurposeUse.Experimental;
#else
		[DefaultValue(PurposeUse.Personal)]
		public PurposeUse Purpose { get; set; } = PurposeUse.Personal;
#endif


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
		/// Any current Identity application.
		/// </summary>
		[DefaultValueNull]
		public LegalIdentity? IdentityApplication { get; set; }

		/// <summary>
		/// Number of peer reviews accepted for the current identity application.
		/// </summary>
		[DefaultValue(0)]
		public int NrReviews { get; set; }

		/// <summary>
		/// Current step in the registration process.
		/// </summary>
		[DefaultValue(RegistrationStep.GetStarted)]
		public RegistrationStep Step { get; set; } = RegistrationStep.GetStarted;

		/// <summary>
		/// Currently selected theme.
		/// </summary>
		[DefaultValueNull]
		public AppTheme? Theme { get; set; }

		/// <summary>
		/// How the user authenticates itself with the App.
		/// </summary>
		[DefaultValue(AuthenticationMethod.Password)]
		public AuthenticationMethod AuthenticationMethod { get; set; } = AuthenticationMethod.Password;

		/// <summary>
		/// If there exist <see cref="ContractReference"/> objects created.
		/// </summary>
		[DefaultValue(false)]
		public bool HasContractReferences { get; set; }

		/// <summary>
		/// If there exist <see cref="ContractReference"/> objects created, referencing contract templates.
		/// </summary>
		[DefaultValue(false)]
		public bool HasContractTemplateReferences { get; set; }

		/// <summary>
		/// If there exist <see cref="ContractReference"/> objects created, referencing contract templates for the creation of tokens.
		/// </summary>
		[DefaultValue(false)]
		public bool HasContractTokenCreationTemplatesReferences { get; set; }

		/// <summary>
		/// If the user has a wallet.
		/// </summary>
		[DefaultValue(false)]
		public bool HasWallet { get; set; }

		/// <summary>
		/// If the user has a thing.
		/// </summary>
		[DefaultValue(false)]
		public bool HasThing { get; set; }

		[DefaultValueNull]
		public DateTime? LastIdentityUpdate { get; set; }
	}
}
