namespace NeuroAccessMaui
{
	/// <summary>
	/// A set of never changing property constants and helpful values.
	/// </summary>
	public static class Constants
	{
		/// <summary>
		/// A generic "no value available" string.
		/// </summary>
		public const string NotAvailableValue = "-";

		/// <summary>
		/// A maximum number of pixels to render for images, downscaling them if necessary.
		/// </summary>
		public const int MaxRenderedImageDimensionInPixels = 800;

		/// <summary>
		/// Application-related constants.
		/// </summary>
		public static class Application
		{
			/// <summary>
			/// Name of application
			/// </summary>
			public const string Name = "Neuro-Access";
		}

		/// <summary>
		/// Authentication constants
		/// </summary>
		public static class Security
		{
			/// <summary>
			/// Minimum length for password
			/// </summary>
			public const int MinPasswordLength = 6;

			/// <summary>
			/// A password score value equal to or higher than this is considered medium security.
			/// </summary>
			public const double MediumSecurityScoreThreshold = 40.0;

			/// <summary>
			/// A password score value equal to or higher than this is considered high security.
			/// </summary>
			public const double HighSecurityPasswordScoreThreshold = 55.0;

			/// <summary>
			/// A password score value equal to or higher than this is considered to be of the highest security.
			/// </summary>
			public const double MaxSecurityPasswordScoreThreshold = 70.0;

			/// <summary>
			/// Minimum number of symbols from at least two character classes (digits, letters, other) in a password.
			/// </summary>
			public const int MinPasswordSymbolsFromDifferentClasses = 0;

			/// <summary>
			/// Maximum number of identical symbols in a password.
			/// </summary>
			public const int MaxPasswordIdenticalSymbols = 6;

			/// <summary>
			/// Maximum number of sequenced symbols in a password.
			/// </summary>
			public const int MaxPasswordSequencedSymbols = 3;

			/// <summary>
			/// Maximum number of seconds screen recording is allowed.
			/// </summary>
			public const int MaxScreenRecordingTimeSeconds = 60 * 60;
		}

		/// <summary>
		/// Language Codes
		/// </summary>
		public static class LanguageCodes
		{
			/// <summary>
			/// The default language code.
			/// </summary>
			public const string Default = "en-US";
		}

		/// <summary>
		/// IoT Schemes
		/// </summary>
		public static class UriSchemes
		{
			/// <summary>
			/// The App's URI Scheme (neuroaccess)
			/// </summary>
			public const string NeuroAccess = "neuroaccess";

			/// <summary>
			/// The IoT ID URI Scheme (iotid)
			/// </summary>
			public const string IotId = "iotid";

			/// <summary>
			/// The IoT Discovery URI Scheme (iotdisco)
			/// </summary>
			public const string IotDisco = "iotdisco";

			/// <summary>
			/// The IoT Smart Contract URI Scheme (iotsc)
			/// </summary>
			public const string IotSc = "iotsc";

			/// <summary>
			/// TAG Signature (Quick-Login) URI Scheme (tagsign)
			/// </summary>
			public const string TagSign = "tagsign";

			/// <summary>
			/// eDaler URI Scheme (edaler)
			/// </summary>
			public const string EDaler = "edaler";

			/// <summary>
			/// eDaler URI Scheme (edaler)
			/// </summary>
			public const string NeuroFeature = "nfeat";

			/// <summary>
			/// Onboarding URI Scheme (obinfo)
			/// </summary>
			public const string Onboarding = "obinfo";

			/// <summary>
			/// XMPP URI Scheme (xmpp)
			/// </summary>
			public const string Xmpp = "xmpp";

			/// <summary>
			/// AES-256-encrypted data.
			/// </summary>
			public const string Aes256 = "aes256";

			/// <summary>
			/// Gets the predefined scheme from an IoT Code
			/// </summary>
			/// <param name="Url">The URL to parse.</param>
			/// <returns>URI Scheme</returns>
			public static string? GetScheme(string Url)
			{
				if (string.IsNullOrWhiteSpace(Url))
					return null;

				int i = Url.IndexOf(':');
				if (i < 0)
					return null;

				Url = Url[..i].ToLowerInvariant();

				return Url switch
				{
					IotId or
					IotDisco or
					IotSc or
					TagSign or
					EDaler or
					NeuroFeature or
					Onboarding or
					Xmpp or
					NeuroAccess => Url,
					_ => null,
				};
			}

			/// <summary>
			/// Checks if the specified code starts with the IoT ID scheme.
			/// </summary>
			/// <param name="Url">The URL to check.</param>
			/// <returns>If URI is an ID scheme</returns>
			public static bool StartsWithIdScheme(string Url)
			{
				return !string.IsNullOrWhiteSpace(Url) &&
					Url.StartsWith(IotId + ":", StringComparison.InvariantCultureIgnoreCase);
			}

			/// <summary>
			/// Generates a IoT Scan Uri form the specified id.
			/// </summary>
			/// <param name="id">The Id to use when generating the Uri.</param>
			/// <returns>Smart Contract URI</returns>
			public static string CreateSmartContractUri(string id)
			{
				return IotSc + ":" + id;
			}

			/// <summary>
			/// Generates a IoT ID Uri form the specified id.
			/// </summary>
			/// <param name="id">The Id to use when generating the Uri.</param>
			/// <returns>Identity URI</returns>
			public static string CreateIdUri(string id)
			{
				return IotId + ":" + id;
			}

			/// <summary>
			/// Generates a Neuro-Feature ID Uri form the specified id.
			/// </summary>
			/// <param name="id">The Id to use when generating the Uri.</param>
			/// <returns>Neuro-Feature URI</returns>
			public static string CreateTokenUri(string id)
			{
				return NeuroFeature + ":" + id;
			}

			/// <summary>
			/// Removes the URI Schema from an URL.
			/// </summary>
			/// <param name="Url">The URL to parse and extract the URI schema from.</param>
			/// <returns>URI, without schema</returns>
			public static string? RemoveScheme(string Url)
			{
				string? Scheme = GetScheme(Url);

				if (string.IsNullOrEmpty(Scheme))
					return null;

				return Url[(Scheme.Length + 1)..];
			}
		}

		/// <summary>
		/// MIME Types
		/// </summary>
		public static class MimeTypes
		{
			/// <summary>
			/// The JPEG MIME type.
			/// </summary>
			public const string Jpeg = "image/jpeg";

			/// <summary>
			/// The PNG MIME type.
			/// </summary>
			public const string Png = "image/png";
		}

		/// <summary>
		/// Domain names.
		/// </summary>
		public static class Domains
		{
			/// <summary>
			/// Neuro-Access domain.
			/// </summary>
			public const string IdDomain = "id.tagroot.io";

			/// <summary>
			/// Neuro-Access onboarding domain.
			/// </summary>
			public const string OnboardingDomain = "onboarding.id.tagroot.io";
		}

		public static class CustomXmppProperties
		{
			/// <summary>
			/// Full birthday  
			/// </summary>
			public const string BirthDay = "FULLBIRTHDAY";
		}

		/// <summary>
		/// XMPP Protocol Properties.
		/// </summary>
		public static class XmppProperties
		{
			/// <summary>
			/// First name
			/// </summary>
			public const string FirstName = "FIRST";

			/// <summary>
			/// Middle names
			/// </summary>
			public const string MiddleNames = "MIDDLE";

			/// <summary>
			/// Last names
			/// </summary>
			public const string LastNames = "LAST";

			/// <summary>
			/// Personal number
			/// </summary>
			public const string PersonalNumber = "PNR";

			/// <summary>
			/// Address line 1
			/// </summary>
			public const string Address = "ADDR";

			/// <summary>
			/// Address line 2
			/// </summary>
			public const string Address2 = "ADDR2";

			/// <summary>
			/// Area
			/// </summary>
			public const string Area = "AREA";

			/// <summary>
			/// City
			/// </summary>
			public const string City = "CITY";

			/// <summary>
			/// Zip Code
			/// </summary>
			public const string ZipCode = "ZIP";

			/// <summary>
			/// Region
			/// </summary>
			public const string Region = "REGION";

			/// <summary>
			/// Country
			/// </summary>
			public const string Country = "COUNTRY";

			/// <summary>
			/// Nationality
			/// </summary>
			public const string Nationality = "NATIONALITY";

			/// <summary>
			/// Gender
			/// </summary>
			public const string Gender = "GENDER";

			/// <summary>
			/// Birth Day
			/// </summary>
			public const string BirthDay = "BDAY";

			/// <summary>
			/// Birth Month
			/// </summary>
			public const string BirthMonth = "BMONTH";

			/// <summary>
			/// Birth Year
			/// </summary>
			public const string BirthYear = "BYEAR";

			/// <summary>
			/// Organization name
			/// </summary>
			public const string OrgName = "ORGNAME";

			/// <summary>
			/// Organization number
			/// </summary>
			public const string OrgNumber = "ORGNR";

			/// <summary>
			/// Organization Address line 1
			/// </summary>
			public const string OrgAddress = "ORGADDR";

			/// <summary>
			/// Organization Address line 2
			/// </summary>
			public const string OrgAddress2 = "ORGADDR2";

			/// <summary>
			/// Organization Area
			/// </summary>
			public const string OrgArea = "ORGAREA";

			/// <summary>
			/// Organization City
			/// </summary>
			public const string OrgCity = "ORGCITY";

			/// <summary>
			/// Organization Zip Code
			/// </summary>
			public const string OrgZipCode = "ORGZIP";

			/// <summary>
			/// Organization Region
			/// </summary>
			public const string OrgRegion = "ORGREGION";

			/// <summary>
			/// Organization Country
			/// </summary>
			public const string OrgCountry = "ORGCOUNTRY";

			/// <summary>
			/// Organization Department
			/// </summary>
			public const string OrgDepartment = "ORGDEPT";

			/// <summary>
			/// Organization Role
			/// </summary>
			public const string OrgRole = "ORGROLE";

			/// <summary>
			/// Device ID
			/// </summary>
			public const string DeviceId = "DEVICE_ID";

			/// <summary>
			/// Jabber ID
			/// </summary>
			public const string Jid = "JID";

			/// <summary>
			/// Phone number
			/// </summary>
			public const string Phone = "PHONE";

			/// <summary>
			/// e-Mail address
			/// </summary>
			public const string EMail = "EMAIL";

			/// <summary>
			/// Domain name.
			/// </summary>
			public const string Domain = "DOMAIN";

			/// <summary>
			/// Apartment
			/// </summary>
			public const string Apartment = "APT";

			/// <summary>
			/// Room
			/// </summary>
			public const string Room = "ROOM";

			/// <summary>
			/// Building
			/// </summary>
			public const string Building = "BLD";

			/// <summary>
			/// Altitude
			/// </summary>
			public const string Altitude = "ALT";

			/// <summary>
			/// Longitude
			/// </summary>
			public const string Longitude = "LON";

			/// <summary>
			/// Latitude
			/// </summary>
			public const string Latitude = "LAT";

			/// <summary>
			/// Class
			/// </summary>
			public const string Class = "CLASS";

			/// <summary>
			/// Key
			/// </summary>
			public const string Key = "KEY";

			/// <summary>
			/// Manufacturer
			/// </summary>
			public const string Manufacturer = "MAN";

			/// <summary>
			/// Meter Location
			/// </summary>
			public const string MeterLocation = "MLOC";

			/// <summary>
			/// MeterNumber
			/// </summary>
			public const string MeterNumber = "MNR";

			/// <summary>
			/// Model
			/// </summary>
			public const string Model = "MODEL";

			/// <summary>
			/// Name
			/// </summary>
			public const string Name = "NAME";

			/// <summary>
			/// Product Information
			/// </summary>
			public const string ProductInformation = "PURL";

			/// <summary>
			/// Registry
			/// </summary>
			public const string Registry = "R";

			/// <summary>
			/// Serial Number
			/// </summary>
			public const string SerialNumber = "SN";

			/// <summary>
			/// Street Name
			/// </summary>
			public const string StreetName = "STREET";

			/// <summary>
			/// Street Number
			/// </summary>
			public const string StreetNumber = "STREETNR";

			/// <summary>
			/// Version
			/// </summary>
			public const string Version = "V";

			/// <summary>
			/// Source ID
			/// </summary>
			public const string SourceId = "SID";

			/// <summary>
			/// Partition
			/// </summary>
			public const string Partition = "PT";

			/// <summary>
			/// Node ID
			/// </summary>
			public const string NodeId = "NID";
		}

		/// <summary>
		/// Generic delay intervals
		/// </summary>
		public static class Delays
		{
			/// <summary>
			/// Default delay interval if waiting for something
			/// </summary>
			public static readonly TimeSpan Default = TimeSpan.FromMilliseconds(50);
		}

		/// <summary>
		/// Timer Intervals
		/// </summary>
		public static class Intervals
		{
			/// <summary>
			/// Auto Save interval
			/// </summary>
			public static readonly TimeSpan AutoSave = TimeSpan.FromSeconds(1);

			/// <summary>
			/// Reconnect interval
			/// </summary>
			public static readonly TimeSpan Reconnect = TimeSpan.FromSeconds(10);

			/// <summary>
			/// Refresh interval for potentially missed messages
			/// </summary>
			public static readonly TimeSpan ForceRefresh = TimeSpan.FromDays(7);
		}

		/// <summary>
		/// Timer Timeout Values
		/// </summary>
		public static class Timeouts
		{
			/// <summary>
			/// Generic request timeout
			/// </summary>
			public static readonly TimeSpan GenericRequest = TimeSpan.FromSeconds(30);

			/// <summary>
			/// Database timeout
			/// </summary>
			public static readonly TimeSpan Database = TimeSpan.FromSeconds(10);

			/// <summary>
			/// XMPP Connect timeout
			/// </summary>
			public static readonly TimeSpan XmppConnect = TimeSpan.FromSeconds(10);

			/// <summary>
			/// XMPP Init timeout
			/// </summary>
			public static readonly TimeSpan XmppInit = TimeSpan.FromSeconds(1);

			/// <summary>
			/// Upload file timeout
			/// </summary>
			public static readonly TimeSpan UploadFile = TimeSpan.FromSeconds(30);

			/// <summary>
			/// Download file timeout
			/// </summary>
			public static readonly TimeSpan DownloadFile = TimeSpan.FromSeconds(10);
		}

		/// <summary>
		/// Push chennels
		/// </summary>
		public static class PushChannels
		{
			/// <summary>
			/// Messages channel
			/// </summary>
			public const string Messages = "Messages";

			/// <summary>
			/// Petitions channel
			/// </summary>
			public const string Petitions = "Petitions";

			/// <summary>
			/// Identities channel
			/// </summary>
			public const string Identities = "Identities";

			/// <summary>
			/// Contracts channel
			/// </summary>
			public const string Contracts = "Contracts";

			/// <summary>
			/// eDaler channel
			/// </summary>
			public const string EDaler = "eDaler";

			/// <summary>
			/// Tokens channel
			/// </summary>
			public const string Tokens = "Tokens";

			/// <summary>
			/// Provisioning channel
			/// </summary>
			public const string Provisioning = "Provisioning";
		}

		/// <summary>
		/// Names of Effects.
		/// </summary>
		public static class Effects
		{
			/// <summary>
			/// ResolutionGroupName used for resolving Effects.
			/// </summary>
			public const string ResolutionGroupName = "com.tag.NeuroAccess";

			/// <summary>
			/// PasswordMaskTogglerEffect.
			/// </summary>
			public const string PasswordMaskTogglerEffect = "PasswordMaskTogglerEffect";
		}

		/// <summary>
		/// Constants for Password
		/// </summary>
		public static class Password
		{

			/// <summary>
			/// Possible time of inactivity
			/// </summary>
			public const int PossibleInactivityInMinutes = 5;

			/// <summary>
			/// Maximum password enetring attempts, first interval
			/// </summary>
			public const int FirstMaxPasswordAttempts = 5;

			/// <summary>
			/// First Block in hours after <see cref="FirstMaxPasswordAttempts"/> attempts
			/// </summary>
			public const int FirstBlockInHours = 1;

			/// <summary>
			/// Maximum password enetring attempts, second interval
			/// </summary>
			public const int SecondMaxPasswordAttempts = 2;

			/// <summary>
			/// Second Block in hours after 3 attempts
			/// </summary>
			public const int SecondBlockInHours = 24;

			/// <summary>
			/// Maximum password enetring attempts, third interval
			/// </summary>
			public const int ThirdMaxPasswordAttempts = 2;

			/// <summary>
			/// Third Block in hours after 3 attempts
			/// </summary>
			public const int ThirdBlockInHours = 7 * 24;

			/// <summary>
			/// Key for password attempt counter
			/// </summary>
			public const string CurrentPasswordAttemptCounter = "CurrentPasswordAttemptCounter";

			/// <summary>
			/// Log Object ID
			/// </summary>
			public const string LogAuditorObjectID = "LogAuditorObjectID";

			/// <summary>
			/// Endpoint for LogAuditor
			/// </summary>
			public const string RemoteEndpoint = "local";

			/// <summary>
			/// Protocol for LogAuditor
			/// </summary>
			public const string Protocol = "local";

			/// <summary>
			/// Reason for LogAuditor
			/// </summary>
			public const string Reason = "pinEnteringFailure";
		}

		/// <summary>
		/// References to external resources
		/// </summary>
		public static class References
		{
			/// <summary>
			/// Resource where Android App can be downloaded.
			/// </summary>
			public const string AndroidApp = "https://play.google.com/store/apps/details?id=com.tag.NeuroAccess";

			/// <summary>
			/// Resource where iPhone App can be downloaded.
			/// </summary>
			public const string IPhoneApp = "https://apps.apple.com/se/app/trust-anchor-access/id1580610247";
		}

		/// <summary>
		/// Absolute paths to important pages.
		/// </summary>
		public static class Pages
		{
			/// <summary>
			/// Path to main page.
			/// </summary>
			public const string MainPage = "//MainPage";

			/// <summary>
			/// Path to registration page.
			/// </summary>
			public const string RegistrationPage = "//Registration";
		}

		/// <summary>
		/// Age-related constants.
		/// </summary>
		public static class Age
		{
			/// <summary>
			/// Minimum age for applying for an ID
			/// </summary>
			public const int MinAge = 13;

			/// <summary>
			/// Maximum age for applying for an ID
			/// </summary>
			public const int MaxAge = 120;
		}

		/// <summary>
		/// Size constants.
		/// </summary>
		public static class BatchSizes
		{
			/// <summary>
			/// Number of messages to load in a single batch.
			/// </summary>
			public const int MessageBatchSize = 30;

			/// <summary>
			/// Number of tokens to load in a single batch.
			/// </summary>
			public const int TokenBatchSize = 10;

			/// <summary>
			/// Number of account events to load in a single batch.
			/// </summary>
			public const int AccountEventBatchSize = 10;

			/// <summary>
			/// Number of devices to load in a single batch.
			/// </summary>
			public const int DeviceBatchSize = 100;
		}

		/// <summary>
		/// Machine-readable names in contracts.
		/// </summary>
		public static class ContractMachineNames
		{
			/// <summary>
			/// Namespace for payment instructions
			/// </summary>
			public const string PaymentInstructionsNamespace = "https://paiwise.tagroot.io/Schema/PaymentInstructions.xsd";

			/// <summary>
			/// Local name for contracts for buying eDaler.
			/// </summary>
			public const string BuyEDaler = "BuyEDaler";

			/// <summary>
			/// Local name for contracts for selling eDaler.
			/// </summary>
			public const string SellEDaler = "SellEDaler";
		}

		/// <summary>
		/// Contract templates
		/// </summary>
		public static class ContractTemplates
		{
			/// <summary>
			/// Contract template for creating a demo token
			/// </summary>
			public const string CreateDemoTokenTemplate = "2bb9fff1-8716-cb1b-5807-9fdb05b2207b@legal.lab.tagroot.io";

			/// <summary>
			/// Contract template for creating five demo tokens
			/// </summary>
			public const string CreateDemoTokens5Template = "2bba00ac-8716-cb3e-5807-9fdb055370c4@legal.lab.tagroot.io";

			/// <summary>
			/// Array of contract templates for creating tokens.
			/// </summary>
			public static readonly string[] TokenCreationTemplates =
			[
				CreateDemoTokenTemplate,
				CreateDemoTokens5Template
			];

			/// <summary>
			/// Contract template for transferring a token from a seller to a buyer
			/// </summary>
			public const string TransferTokenTemplate = "2a6d6b09-cae9-bb7e-4015-a272cd9cd5b9@legal.lab.tagroot.io";

			/// <summary>
			/// Contract template for consigning the token to an auctioneer with the purpose of selling it.
			/// </summary>
			public const string TokenConsignmentTemplate = "2a6d86d3-cae9-be05-4015-a272cd0cbbb9@legal.lab.tagroot.io";
		}

		/// <summary>
		/// Image resources.
		/// </summary>
		public static class Images
		{
			/// <summary>
			/// QR-code with a person icon.
			/// </summary>
			public const string Qr_Person = "file://qr_person.svg";
		}

		/// <summary>
		/// QR Code constants.
		/// </summary>
		public static class QrCode
		{
			/// <summary>
			/// The default width to use when generating QR Code images.
			/// </summary>
			public const int DefaultImageWidth = 240;
			/// <summary>
			/// The default height to use when generating QR Code images.
			/// </summary>
			public const int DefaultImageHeight = 240;
			/// <summary>
			/// The default scale factor to apply to the QR Code image resolution.
			/// </summary>
			public const int DefaultResolutionScale = 2;
		}

		/// <summary>
		/// Runtime setting key names.
		/// </summary>
		public static class Settings
		{
			/// <summary>
			/// Push-notification configuration version.
			/// </summary>
			public const string PushNotificationConfigurationVersion = "PUSH.CONFIG_VERSION";

			/// <summary>
			/// When push-notification token was reported.
			/// </summary>
			public const string PushNotificationReportDate = "PUSH.REPORT_DATE";

			/// <summary>
			/// Push-notification token.
			/// </summary>
			public const string PushNotificationToken = "PUSH.TOKEN";

			/// <summary>
			/// Transfer ID code
			/// </summary>
			public const string TransferIdCodeSent = "TransferId.CodesSent";
		}

		/// <summary>
		/// Contains intent action constants used for inter-component communication within the app.
		/// </summary>
		public static class IntentActions
		{
			/// <summary>
			/// Action used to open a URL, typically triggered by deep linking.
			/// </summary>
			public const string OpenUrl = "OpenUrl";

			/// <summary>
			/// Action triggered when an NFC tag is discovered.
			/// </summary>
			public const string NfcTagDiscovered = "NfcTagDiscovered";

			/// <summary>
			/// Action triggered when the app needs to navigate to a specific page.
			/// </summary>
			public const string Navigate = "Navigate";

			// Add additional intent actions as needed.
		}
	}
}
