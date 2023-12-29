using NeuroAccessMaui.Extensions;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace NeuroAccessMaui.Services.Tag
{
	/// <inheritdoc/>
	[Singleton]
	public partial class TagProfile : ITagProfile
	{
		private readonly WeakEventManager onStepChangedEventManager = new();
		private readonly WeakEventManager onChangedEventManager = new();

		/// <summary>
		/// An event that fires every time the <see cref="Step"/> property changes.
		/// </summary>
		public event EventHandler? StepChanged
		{
			add => this.onStepChangedEventManager.AddEventHandler(value);
			remove => this.onStepChangedEventManager.RemoveEventHandler(value);
		}

		/// <summary>
		/// An event that fires every time any property changes.
		/// </summary>
		public event PropertyChangedEventHandler? Changed
		{
			add => this.onChangedEventManager.AddEventHandler(value);
			remove => this.onChangedEventManager.RemoveEventHandler(value);
		}

		private LegalIdentity? legalIdentity;
		private string? objectId;
		private string? initialDomain;
		private string? initialApiKey;
		private string? initialApiSecret;
		private string? domain;
		private string? apiKey;
		private string? apiSecret;
		private string? selectedCountry;
		private string? phoneNumber;
		private string? eMail;
		private string? account;
		private string? passwordHash;
		private string? passwordHashMethod;
		private string? legalJid;
		private string? httpFileUploadJid;
		private string? logJid;
		private string? mucJid;
		private string? pinHash;
		private long httpFileUploadMaxSize;
		private bool isTest;
		private PurposeUse purpose;
		private DateTime? testOtpTimestamp;
		private RegistrationStep step = RegistrationStep.RequestPurpose;
		private AppTheme? theme;
		private AuthenticationMethod authenticationMethod;
		private bool suppressPropertyChangedEvents;
		private bool initialDefaultXmppConnectivity;
		private bool defaultXmppConnectivity;

		/// <summary>
		/// Creates an instance of a <see cref="TagProfile"/>.
		/// </summary>
		public TagProfile()
		{
		}

		/// <summary>
		/// Invoked whenever the current <see cref="Step"/> changes, to fire the <see cref="StepChanged"/> event.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnStepChanged(EventArgs e)
		{
			this.onStepChangedEventManager.HandleEvent(this, EventArgs.Empty, nameof(StepChanged));
		}

		/// <summary>
		/// Invoked whenever any property changes, to fire the <see cref="Changed"/> event.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnChanged(PropertyChangedEventArgs e)
		{
			this.onChangedEventManager.HandleEvent(this, e, nameof(Changed));
		}

		/// <summary>
		/// Converts the current instance into a <see cref="TagConfiguration"/> object for serialization.
		/// </summary>
		/// <returns>Configuration object</returns>
		public TagConfiguration ToConfiguration()
		{
			TagConfiguration Clone = new()
			{
				ObjectId = this.objectId,
				InitialDefaultXmppConnectivity = this.InitialDefaultXmppConnectivity,
				InitialDomain = this.InitialDomain,
				InitialApiKey = this.InitialApiKey,
				InitialApiSecret = this.InitialApiSecret,
				Domain = this.Domain,
				ApiKey = this.ApiKey,
				ApiSecret = this.ApiSecret,
				SelectedCountry = this.SelectedCountry,
				PhoneNumber = this.PhoneNumber,
				EMail = this.EMail,
				DefaultXmppConnectivity = this.DefaultXmppConnectivity,
				Account = this.Account,
				PasswordHash = this.PasswordHash,
				PasswordHashMethod = this.PasswordHashMethod,
				LegalJid = this.LegalJid,
				HttpFileUploadJid = this.HttpFileUploadJid,
				HttpFileUploadMaxSize = this.HttpFileUploadMaxSize,
				LogJid = this.LogJid,
				PinHash = this.PinHash,
				IsTest = this.IsTest,
				Purpose = this.Purpose,
				TestOtpTimestamp = this.TestOtpTimestamp,
				LegalIdentity = this.LegalIdentity,
				Step = this.Step,
				Theme = this.Theme,
				AuthenticationMethod = this.AuthenticationMethod
			};

			return Clone;
		}

		/// <summary>
		/// Parses an instance of a <see cref="TagConfiguration"/> object to update this instance's properties.
		/// </summary>
		/// <param name="configuration"></param>
		public void FromConfiguration(TagConfiguration configuration)
		{
			try
			{
				this.suppressPropertyChangedEvents = true;

				this.objectId = configuration.ObjectId;
				this.InitialDomain = configuration.InitialDomain;
				this.InitialApiKey = configuration.InitialApiKey;
				this.InitialApiSecret = configuration.InitialApiSecret;
				this.Domain = configuration.Domain;
				this.ApiKey = configuration.ApiKey;
				this.ApiSecret = configuration.ApiSecret;
				this.SelectedCountry = configuration.SelectedCountry;
				this.PhoneNumber = configuration.PhoneNumber;
				this.EMail = configuration.EMail;
				this.InitialDefaultXmppConnectivity = configuration.InitialDefaultXmppConnectivity;
				this.DefaultXmppConnectivity = configuration.DefaultXmppConnectivity;
				this.Account = configuration.Account;
				this.PasswordHash = configuration.PasswordHash;
				this.PasswordHashMethod = configuration.PasswordHashMethod;
				this.LegalJid = configuration.LegalJid;
				this.HttpFileUploadJid = configuration.HttpFileUploadJid;
				this.HttpFileUploadMaxSize = configuration.HttpFileUploadMaxSize;
				this.LogJid = configuration.LogJid;
				this.PinHash = configuration.PinHash;
				this.IsTest = configuration.IsTest;
				this.Purpose = configuration.Purpose;
				this.TestOtpTimestamp = configuration.TestOtpTimestamp;
				this.LegalIdentity = configuration.LegalIdentity;
				this.Theme = configuration.Theme;
				this.AuthenticationMethod = configuration.AuthenticationMethod;

				// Do this last, as listeners will read the other properties when the event is fired.
				this.GoToStep(configuration.Step);
			}
			finally
			{
				this.suppressPropertyChangedEvents = false;
			}
		}

		/// <summary>
		/// Returns <c>true</c> if the current <see cref="ITagProfile"/> needs to have its values updated, <c>false</c> otherwise.
		/// </summary>
		/// <returns>If values need updating</returns>
		public virtual bool NeedsUpdating()
		{
			return string.IsNullOrWhiteSpace(this.legalJid) ||
				   string.IsNullOrWhiteSpace(this.httpFileUploadJid) ||
				   string.IsNullOrWhiteSpace(this.logJid) ||
				   string.IsNullOrWhiteSpace(this.mucJid);
		}

		/// <summary>
		/// Returns <c>true</c> if the current <see cref="ITagProfile"/> needs to have its legal identity updated, <c>false</c> otherwise.
		/// </summary>
		/// <returns>If legal identity need updating</returns>
		public virtual bool LegalIdentityNeedsUpdating()
		{
			return this.legalIdentity?.Discarded() ?? true;
		}

		/// <summary>
		/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> has an account but not a legal id,
		/// <c>false</c> otherwise.
		/// </summary>
		/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> has an account but not a legal id,
		/// <c>false</c> otherwise.</returns>
		public virtual bool ShouldCreateClient()
		{
			return this.Step >= RegistrationStep.CreateAccount && !string.IsNullOrEmpty(this.Account);
		}

		/// <summary>
		/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete or is just awaiting validation, <c>false</c> otherwise.
		/// </summary>
		/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete or is just awaiting validation, <c>false</c> otherwise.</returns>
		public virtual bool IsCompleteOrWaitingForValidation()
		{
			return this.Step >= RegistrationStep.CreateAccount &&
				this.LegalIdentity is not null &&
				this.LegalIdentity.State == IdentityState.Created;
		}

		/// <summary>
		/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete, <c>false</c> otherwise.
		/// </summary>
		/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete, <c>false</c> otherwise.</returns>
		public virtual bool IsComplete()
		{
			return this.Step == RegistrationStep.Complete;
		}

		#region Properties

		/// <summary>
		/// Initial domain.
		/// </summary>
		public string? InitialDomain
		{
			get => this.initialDomain;
			private set
			{
				if (!string.Equals(this.initialDomain, value, StringComparison.Ordinal))
				{
					this.initialDomain = value;
					this.FlagAsDirty(nameof(this.InitialDomain));
				}
			}
		}

		/// <summary>
		/// If the <see cref="InitialDomain"/> employs default XMPP connectivity.
		/// </summary>
		public bool InitialDefaultXmppConnectivity
		{
			get => this.initialDefaultXmppConnectivity;
			private set
			{
				if (this.initialDefaultXmppConnectivity != value)
				{
					this.initialDefaultXmppConnectivity = value;
					this.FlagAsDirty(nameof(this.InitialDefaultXmppConnectivity));
				}
			}
		}

		/// <summary>
		/// API Key for the <see cref="InitialDomain"/>
		/// </summary>
		public string? InitialApiKey
		{
			get => this.initialApiKey;
			private set
			{
				if (!string.Equals(this.initialApiKey, value, StringComparison.Ordinal))
				{
					this.initialApiKey = value;
					this.FlagAsDirty(nameof(this.InitialApiKey));
				}
			}
		}

		/// <summary>
		/// Secret for the <see cref="InitialApiKey"/>
		/// </summary>
		public string? InitialApiSecret
		{
			get => this.initialApiSecret;
			private set
			{
				if (!string.Equals(this.initialApiSecret, value, StringComparison.Ordinal))
				{
					this.initialApiSecret = value;
					this.FlagAsDirty(nameof(this.InitialApiSecret));
				}
			}
		}

		/// <summary>
		/// The domain this profile is connected to.
		/// </summary>
		public string? Domain
		{
			get => this.domain;
			private set
			{
				if (!string.Equals(this.domain, value, StringComparison.Ordinal))
				{
					this.domain = value;
					this.FlagAsDirty(nameof(this.Domain));
				}
			}
		}

		/// <summary>
		/// If connecting to the domain can be done using default parameters (host=domain, default c2s port).
		/// </summary>
		public bool DefaultXmppConnectivity
		{
			get => this.defaultXmppConnectivity;
			private set
			{
				if (this.defaultXmppConnectivity != value)
				{
					this.defaultXmppConnectivity = value;
					this.FlagAsDirty(nameof(this.DefaultXmppConnectivity));
				}
			}
		}

		/// <summary>
		/// API Key, for creating new account.
		/// </summary>
		public string? ApiKey
		{
			get => this.apiKey;
			private set
			{
				if (!string.Equals(this.apiKey, value, StringComparison.Ordinal))
				{
					this.apiKey = value;
					this.FlagAsDirty(nameof(this.ApiKey));
				}
			}
		}

		/// <summary>
		/// API Secret, for creating new account.
		/// </summary>
		public string? ApiSecret
		{
			get => this.apiSecret;
			private set
			{
				if (!string.Equals(this.apiSecret, value, StringComparison.Ordinal))
				{
					this.apiSecret = value;
					this.FlagAsDirty(nameof(this.ApiSecret));
				}
			}
		}

		/// <summary>
		/// Selected country. Some countries have the same phone code, so we want to save the selected country
		/// </summary>
		public string? SelectedCountry
		{
			get => this.selectedCountry;
			private set
			{
				if (!string.Equals(this.selectedCountry, value, StringComparison.Ordinal))
				{
					this.selectedCountry = value;
					this.FlagAsDirty(nameof(this.SelectedCountry));
				}
			}
		}

		/// <summary>
		/// Verified phone number.
		/// </summary>
		public string? PhoneNumber
		{
			get => this.phoneNumber;
			private set
			{
				if (!string.Equals(this.phoneNumber, value, StringComparison.Ordinal))
				{
					this.phoneNumber = value;
					this.FlagAsDirty(nameof(this.PhoneNumber));
				}
			}
		}

		/// <summary>
		/// Verified e-mail address.
		/// </summary>
		public string? EMail
		{
			get => this.eMail;
			private set
			{
				if (!string.Equals(this.eMail, value, StringComparison.Ordinal))
				{
					this.eMail = value;
					this.FlagAsDirty(nameof(this.EMail));
				}
			}
		}

		/// <summary>
		/// The account name for this profile
		/// </summary>
		public string? Account
		{
			get => this.account;
			private set
			{
				if (!string.Equals(this.account, value, StringComparison.Ordinal))
				{
					this.account = value;
					this.FlagAsDirty(nameof(this.Account));
				}
			}
		}

		/// <summary>
		/// A hash of the current password.
		/// </summary>
		public string? PasswordHash
		{
			get => this.passwordHash;
			private set
			{
				if (!string.Equals(this.passwordHash, value, StringComparison.Ordinal))
				{
					this.passwordHash = value;
					this.FlagAsDirty(nameof(this.PasswordHash));
				}
			}
		}

		/// <summary>
		/// The hash method used for hashing the password.
		/// </summary>
		public string? PasswordHashMethod
		{
			get => this.passwordHashMethod;
			private set
			{
				if (!string.Equals(this.passwordHashMethod, value, StringComparison.Ordinal))
				{
					this.passwordHashMethod = value;
					this.FlagAsDirty(nameof(this.PasswordHashMethod));
				}
			}
		}

		/// <summary>
		/// The Jabber Legal JID for this user/profile.
		/// </summary>
		public string? LegalJid
		{
			get => this.legalJid;
			private set
			{
				if (!string.Equals(this.legalJid, value, StringComparison.Ordinal))
				{
					this.legalJid = value;
					this.FlagAsDirty(nameof(this.LegalJid));
				}
			}
		}

		/// <summary>
		/// The XMPP server's file upload Jid.
		/// </summary>
		public string? HttpFileUploadJid
		{
			get => this.httpFileUploadJid;
			private set
			{
				if (!string.Equals(this.httpFileUploadJid, value, StringComparison.Ordinal))
				{
					this.httpFileUploadJid = value;
					this.FlagAsDirty(nameof(this.HttpFileUploadJid));
				}
			}
		}

		/// <summary>
		/// The XMPP server's max size for file uploads.
		/// </summary>
		public long HttpFileUploadMaxSize
		{
			get => this.httpFileUploadMaxSize;
			private set
			{
				if (this.httpFileUploadMaxSize != value)
				{
					this.httpFileUploadMaxSize = value;
					this.FlagAsDirty(nameof(this.HttpFileUploadMaxSize));
				}
			}
		}

		/// <summary>
		/// The XMPP server's log Jid.
		/// </summary>
		public string? LogJid
		{
			get => this.logJid;
			private set
			{
				if (!string.Equals(this.logJid, value, StringComparison.Ordinal))
				{
					this.logJid = value;
					this.FlagAsDirty(nameof(this.LogJid));
				}
			}
		}

		/// <summary>
		/// This profile's current registration step.
		/// </summary>
		public RegistrationStep Step => this.step;

		/// <summary>
		/// Changes the current onboarding step.
		/// </summary>
		/// <param name="NewStep">New step</param>
		/// <param name="SupressEvent">If registration step event should be supressed (default=false).</param>
		public void GoToStep(RegistrationStep NewStep, bool SupressEvent = false)
		{
			if (this.step != NewStep)
			{
				this.step = NewStep;
				this.FlagAsDirty(nameof(this.Step));

				if (!SupressEvent)
					this.OnStepChanged(EventArgs.Empty);
			}
		}

		/// <summary>
		/// Returns <c>true</c> if file upload is supported for the specified XMPP server, <c>false</c> otherwise.
		/// </summary>
		public bool FileUploadIsSupported
		{
			get => !string.IsNullOrWhiteSpace(this.HttpFileUploadJid) && this.HttpFileUploadMaxSize > 0;
		}

		/// <summary>
		/// The user's PIN value.
		/// </summary>
		public string Pin
		{
			set => this.PinHash = this.ComputePinHash(value);
		}

		/// <summary>
		/// A hashed version of the user's <see cref="Pin"/>.
		/// </summary>
		public string? PinHash
		{
			get => this.pinHash;
			private set
			{
				if (!string.Equals(this.pinHash, value, StringComparison.Ordinal))
				{
					this.pinHash = value;
					this.FlagAsDirty(nameof(this.PinHash));
				}
			}
		}

		/// <summary>
		/// Indicates if the user has a <see cref="Pin"/>.
		/// </summary>
		public bool HasPin
		{
			get => !string.IsNullOrEmpty(this.PinHash);
		}

		/// <summary>
		/// How the user authenticates itself with the App.
		/// </summary>
		public AuthenticationMethod AuthenticationMethod
		{
			get => this.authenticationMethod;
			private set
			{
				if (this.authenticationMethod != value)
				{
					this.authenticationMethod = value;
					this.FlagAsDirty(nameof(this.AuthenticationMethod));
				}
			}
		}

		/// <summary>
		/// Returns <c>true</c> if the user choose the educational or experimental purpose.
		/// </summary>
		public bool IsTest
		{
			get => this.isTest;
			private set
			{
				if (this.isTest != value)
				{
					this.isTest = value;
					this.FlagAsDirty(nameof(this.IsTest));
				}
			}
		}

		/// <summary>
		/// Purpose for using the app.
		/// </summary>
		public PurposeUse Purpose
		{
			get => this.purpose;
			private set
			{
				if (this.purpose != value)
				{
					this.purpose = value;
					this.FlagAsDirty(nameof(this.Purpose));
				}
			}
		}

		/// <summary>
		/// Returns a timestamp if the user used a Test OTP Code.
		/// </summary>
		public DateTime? TestOtpTimestamp
		{
			get => this.testOtpTimestamp;
			private set
			{
				if (this.testOtpTimestamp != value)
				{
					this.testOtpTimestamp = value;
					this.FlagAsDirty(nameof(this.TestOtpTimestamp));
				}
			}
		}

		/// <summary>
		/// The legal identity of the curren user/profile.
		/// </summary>
		public LegalIdentity? LegalIdentity
		{
			get => this.legalIdentity;
			private set
			{
				if (!Equals(this.legalIdentity, value))
				{
					this.legalIdentity = value;
					this.FlagAsDirty(nameof(this.LegalIdentity));
				}
			}
		}

		/// <summary>
		/// Currently selected theme.
		/// </summary>
		public AppTheme? Theme
		{
			get => this.theme;
			private set
			{
				if (!Equals(this.theme, value))
				{
					this.theme = value;
					this.FlagAsDirty(nameof(this.Theme));
				}
			}
		}

		/// <summary>
		/// Returns <c>true</c> if the current <see cref="ITagProfile"/> has changed values and need saving, <c>false</c> otherwise.
		/// </summary>
		public bool IsDirty { get; private set; }

		private void FlagAsDirty(string PropertyName)
		{
			this.IsDirty = true;

			if (!this.suppressPropertyChangedEvents)
				this.OnChanged(new PropertyChangedEventArgs(PropertyName));
		}

		/// <summary>
		/// Resets the <see cref="IsDirty"/> flag, can be used after persisting values to <see cref="IStorageService"/>.
		/// </summary>
		public void ResetIsDirty()
		{
			this.IsDirty = false;
		}

		#endregion

		#region Build Steps

		/// <summary>
		/// Sets the phone number used for contacting the user.
		/// </summary>
		/// <param name="Country">Country of the phone number.</param>
		/// <param name="PhoneNumber">Verified phone number.</param>
		public void SetPhone(string Country, string PhoneNumber)
		{
			this.SelectedCountry = Country;
			this.PhoneNumber = PhoneNumber;
		}

		/// <summary>
		/// Sets the e-mail address used for contacting the user.
		/// </summary>
		/// <param name="EMail">Verified e-mail address.</param>
		public void SetEMail(string EMail)
		{
			this.EMail = EMail;
		}

		/// <summary>
		/// Set the domain name to connect to.
		/// </summary>
		/// <param name="DomainName">The domain name.</param>
		/// <param name="DefaultXmppConnectivity">If connecting to the domain can be done using default parameters (host=domain, default c2s port).</param>
		/// <param name="Key">Key to use, if an account is to be created.</param>
		/// <param name="Secret">Secret to use, if an account is to be created.</param>
		public void SetDomain(string DomainName, bool DefaultXmppConnectivity, string Key, string Secret)
		{
			if (string.IsNullOrEmpty(this.InitialDomain))
			{
				this.InitialDefaultXmppConnectivity = this.DefaultXmppConnectivity;
				this.InitialDomain = this.Domain;
				this.InitialApiKey = this.ApiKey;
				this.InitialApiSecret = this.ApiSecret;
			}

			if (string.IsNullOrEmpty(this.InitialDomain))
			{
				this.InitialDefaultXmppConnectivity = DefaultXmppConnectivity;
				this.InitialDomain = DomainName;
				this.InitialApiKey = Key;
				this.InitialApiSecret = Secret;
			}

			this.DefaultXmppConnectivity = DefaultXmppConnectivity;
			this.Domain = DomainName;
			this.ApiKey = Key;
			this.ApiSecret = Secret;
		}

		/// <summary>
		/// Reverses the SetDomain to the Initial* values.
		/// </summary>
		public void UndoDomainSelection()
		{
			if (this.InitialDomain is not null)
			{
				this.DefaultXmppConnectivity = this.InitialDefaultXmppConnectivity;
				this.Domain = this.InitialDomain;
				this.ApiKey = this.InitialApiKey;
				this.ApiSecret = this.InitialApiSecret;
			}
		}

		/// <summary>
		/// Revert the SetDomain
		/// </summary>
		public void ClearDomain()
		{
			this.DefaultXmppConnectivity = false;
			this.Domain = string.Empty;
			this.ApiKey = string.Empty;
			this.ApiSecret = string.Empty;
		}

		/// <summary>
		/// Set the account name and password for a <em>new</em> account.
		/// </summary>
		/// <param name="AccountName">The account/user name.</param>
		/// <param name="ClientPasswordHash">The password hash (never send the real password).</param>
		/// <param name="ClientPasswordHashMethod">The hash method used when hashing the password.</param>
		public void SetAccount(string AccountName, string ClientPasswordHash, string ClientPasswordHashMethod)
		{
			this.PasswordHash = ClientPasswordHash;
			this.PasswordHashMethod = ClientPasswordHashMethod;
			this.ApiKey = string.Empty;
			this.ApiSecret = string.Empty;

			// It's important for this to be the last, since it will fire the account change notification.
			this.Account = AccountName;
		}

		/// <summary>
		/// Set the account name and password for an <em>existing</em> account.
		/// </summary>
		/// <param name="AccountName">The account/user name.</param>
		/// <param name="ClientPasswordHash">The password hash (never send the real password).</param>
		/// <param name="ClientPasswordHashMethod">The hash method used when hashing the password.</param>
		/// <param name="Identity">The new identity.</param>
		public void SetAccountAndLegalIdentity(string AccountName, string ClientPasswordHash, string ClientPasswordHashMethod, LegalIdentity Identity)
		{
			this.PasswordHash = ClientPasswordHash;
			this.PasswordHashMethod = ClientPasswordHashMethod;
			this.ApiKey = string.Empty;
			this.ApiSecret = string.Empty;
			this.LegalIdentity = Identity;

			// It's important for this to be the last, since it will fire the account change notification.
			this.Account = AccountName;
		}

		/// <summary>
		/// Set the legal identity of a newly created account.
		/// </summary>
		/// <param name="LegalIdentity">The legal identity to use.</param>
		public void SetLegalIdentity(LegalIdentity Identity)
		{
			this.LegalIdentity = Identity;
		}

		/// <summary>
		/// Revert the Set Account
		/// </summary>
		public void ClearAccount()
		{
			this.PasswordHash = string.Empty;
			this.PasswordHashMethod = string.Empty;
			this.LegalJid = string.Empty;

			// It's important for this to be the last, since it will fire the account change notification.
			this.Account = string.Empty;
		}

		/// <summary>
		/// Revert the Set LegalIdentity
		/// </summary>
		public void ClearLegalIdentity()
		{
			this.LegalIdentity = null;
		}

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the revoked identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="revokedIdentity">The revoked identity to use.</param>
		public void RevokeLegalIdentity(LegalIdentity RevokedIdentity)
		{
			this.LegalIdentity = RevokedIdentity;
		}

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the compromised identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="compromisedIdentity">The compromised identity to use.</param>
		public void CompromiseLegalIdentity(LegalIdentity CompromisedIdentity)
		{
			this.LegalIdentity = CompromisedIdentity;
		}

		/// <summary>
		/// Set a pin to use for protecting the account.
		/// </summary>
		/// <param name="Pin">The pin to use.</param>
		public void SetPin(string Pin)
		{
			this.Pin = Pin;
		}

		/// <summary>
		/// Set if the user choose the educational or experimental purpose.
		/// </summary>
		/// <param name="IsTest">If app is in test mode.</param>
		/// <param name="Purpose">Purpose for using the app</param>
		public void SetPurpose(bool IsTest, PurposeUse Purpose)
		{
			this.IsTest = IsTest;
			this.Purpose = Purpose;
		}

		/// <inheritdoc/>
		public void SetTestOtpTimestamp(DateTime? Timestamp)
		{
			this.TestOtpTimestamp = Timestamp;
		}

		/// <summary>
		/// Used during XMPP service discovery. Sets the legal id.
		/// </summary>
		/// <param name="legalJid">The legal id.</param>
		public void SetLegalJid(string LegalJid)
		{
			this.LegalJid = LegalJid;
		}

		/// <summary>
		/// Used during XMPP service discovery. Sets the file upload parameters.
		/// </summary>
		/// <param name="httpFileUploadJid">The http file upload id.</param>
		/// <param name="maxSize">The max size allowed.</param>
		public void SetFileUploadParameters(string HttpFileUploadJid, long MaxSize)
		{
			this.HttpFileUploadJid = HttpFileUploadJid;
			this.HttpFileUploadMaxSize = MaxSize;
		}

		/// <summary>
		/// Used during XMPP service discovery. Sets the Log ID.
		/// </summary>
		/// <param name="logJid">The log id.</param>
		public void SetLogJid(string LogJid)
		{
			this.LogJid = LogJid;
		}

		/// <summary>
		/// Sets the preferred theme.
		/// </summary>
		/// <param name="Theme">Theme</param>
		public void SetTheme(AppTheme Theme)
		{
			this.Theme = Theme;
			this.SetTheme();
		}

		/// <summary>
		/// Sets the preferred theme.
		/// </summary>
		public void SetTheme()
		{
			if (Application.Current is not null && this.Theme.HasValue)
				Application.Current.UserAppTheme = this.Theme.Value;
		}

		/// <summary>
		/// Sets the authentication method.
		/// </summary>
		/// <param name="AuthenticationMethod">Authentication method.</param>
		public void SetAuthenticationMethod(AuthenticationMethod AuthenticationMethod)
		{
			this.authenticationMethod = AuthenticationMethod;
		}

		#endregion

		/// <inheritdoc/>
		public string ComputePinHash(string Pin)
		{
			StringBuilder sb = new();

			sb.Append(this.objectId);
			sb.Append(':');
			sb.Append(this.domain);
			sb.Append(':');
			sb.Append(this.account);
			sb.Append(':');
			sb.Append(this.legalJid);
			sb.Append(':');
			sb.Append(Pin);

			string s = sb.ToString();
			byte[] Data = Encoding.UTF8.GetBytes(s);

			return Hashes.ComputeSHA384HashString(Data);
		}

		/// <summary>
		/// Clears the entire profile.
		/// </summary>
		public void ClearAll()
		{
			this.legalIdentity = null;
			this.domain = string.Empty;
			this.apiKey = string.Empty;
			this.apiSecret = string.Empty;
			this.selectedCountry = string.Empty;
			this.phoneNumber = string.Empty;
			this.eMail = string.Empty;
			this.account = string.Empty;
			this.passwordHash = string.Empty;
			this.passwordHashMethod = string.Empty;
			this.legalJid = string.Empty;
			this.httpFileUploadJid = string.Empty;
			this.logJid = string.Empty;
			this.mucJid = string.Empty;
			this.pinHash = string.Empty;
			this.httpFileUploadMaxSize = 0;
			this.isTest = false;
			this.TestOtpTimestamp = null;
			this.step = RegistrationStep.RequestPurpose;
			this.defaultXmppConnectivity = false;

			this.IsDirty = true;
		}

		/// <inheritdoc/>
		public PinStrength ValidatePinStrength(string? Pin)
		{
			if (Pin is null)
			{
				return PinStrength.NotEnoughDigitsLettersSigns;
			}

			Pin = Pin.Normalize();

			int DigitsCount = 0;
			int LettersCount = 0;
			int SignsCount = 0;

			Dictionary<int, int> DistinctSymbolsCount = [];

			int[] SlidingWindow = new int[Constants.Security.MaxPinSequencedSymbols + 1];
			SlidingWindow.Initialize();

			for (int i = 0; i < Pin.Length;)
			{
				if (char.IsDigit(Pin, i))
					DigitsCount++;
				else if (char.IsLetter(Pin, i))
					LettersCount++;
				else
					SignsCount++;

				int Symbol = char.ConvertToUtf32(Pin, i);

				if (DistinctSymbolsCount.TryGetValue(Symbol, out int SymbolCount))
				{
					DistinctSymbolsCount[Symbol] = ++SymbolCount;
					if (SymbolCount > Constants.Security.MaxPinIdenticalSymbols)
						return PinStrength.TooManyIdenticalSymbols;
				}
				else
					DistinctSymbolsCount.Add(Symbol, 1);

				for (int j = 0; j < SlidingWindow.Length - 1; j++)
					SlidingWindow[j] = SlidingWindow[j + 1];

				SlidingWindow[^1] = Symbol;

				int[] SlidingWindowDifferences = new int[SlidingWindow.Length - 1];
				for (int j = 0; j < SlidingWindow.Length - 1; j++)
					SlidingWindowDifferences[j] = SlidingWindow[j + 1] - SlidingWindow[j];

				if (SlidingWindowDifferences.All(difference => difference == 1))
					return PinStrength.TooManySequencedSymbols;

				if (char.IsSurrogate(Pin, i))
					i += 2;
				else
					i += 1;
			}

			if (this.LegalIdentity is LegalIdentity LegalIdentity)
			{
				const StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase;

				if (LegalIdentity[Constants.XmppProperties.PersonalNumber] is string PersonalNumber && PersonalNumber != "" && Pin.Contains(PersonalNumber, Comparison))
					return PinStrength.ContainsPersonalNumber;

				if (LegalIdentity[Constants.XmppProperties.Phone] is string Phone && !string.IsNullOrEmpty(Phone) && Pin.Contains(Phone, Comparison))
					return PinStrength.ContainsPhoneNumber;

				if (LegalIdentity[Constants.XmppProperties.EMail] is string EMail && !string.IsNullOrEmpty(EMail) && Pin.Contains(EMail, Comparison))
					return PinStrength.ContainsEMail;

				IEnumerable<string> NameWords = new string[]
				{
					Constants.XmppProperties.FirstName,
					Constants.XmppProperties.MiddleName,
					Constants.XmppProperties.LastName,
				}
				.SelectMany(PropertyKey => LegalIdentity[PropertyKey] is string PropertyValue ? PropertyValueSplitRegex().Split(PropertyValue) : Enumerable.Empty<string>())
				.Where(Word => Word?.GetUnicodeLength() > 2);

				if (NameWords.Any(NameWord => Pin.Contains(NameWord, Comparison)))
					return PinStrength.ContainsName;

				IEnumerable<string> AddressWords = new string[]
				{
					Constants.XmppProperties.Address,
					Constants.XmppProperties.Address2,
				}
				.SelectMany(PropertyKey => LegalIdentity[PropertyKey] is string PropertyValue ? PropertyValueSplitRegex().Split(PropertyValue) : Enumerable.Empty<string>())
				.Where(Word => Word?.GetUnicodeLength() > 2);

				if (AddressWords.Any(AddressWord => Pin.Contains(AddressWord, Comparison)))
					return PinStrength.ContainsAddress;
			}

			const int MinDigitsCount = Constants.Security.MinPinSymbolsFromDifferentClasses;
			const int MinLettersCount = Constants.Security.MinPinSymbolsFromDifferentClasses;
			const int MinSignsCount = Constants.Security.MinPinSymbolsFromDifferentClasses;

			if (DigitsCount < MinDigitsCount && LettersCount < MinLettersCount && SignsCount < MinSignsCount)
				return PinStrength.NotEnoughDigitsLettersSigns;

			if (DigitsCount >= MinDigitsCount && LettersCount < MinLettersCount && SignsCount < MinSignsCount)
				return PinStrength.NotEnoughLettersOrSigns;

			if (DigitsCount < MinDigitsCount && LettersCount >= MinLettersCount && SignsCount < MinSignsCount)
				return PinStrength.NotEnoughDigitsOrSigns;

			if (DigitsCount < MinDigitsCount && LettersCount < MinLettersCount && SignsCount >= MinSignsCount)
				return PinStrength.NotEnoughLettersOrDigits;

			if (DigitsCount + LettersCount + SignsCount < Constants.Security.MinPinLength)
				return PinStrength.TooShort;

			return PinStrength.Strong;
		}

		[GeneratedRegex(@"\p{Zs}+")]
		private static partial Regex PropertyValueSplitRegex();
	}
}
