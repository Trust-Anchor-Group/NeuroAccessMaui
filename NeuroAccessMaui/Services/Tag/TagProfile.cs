using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Storage;
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
		private LegalIdentity? identityApplication;
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
		private string? registryJid;
		private string? provisioningJid;
		private string? httpFileUploadJid;
		private string? logJid;
		private string? eDalerJid;
		private string? neuroFeaturesJid;
		private string? pinHash;
		private long httpFileUploadMaxSize;
		private bool supportsPushNotification;
		private int nrReviews;
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
				RegistryJid = this.RegistryJid,
				ProvisioningJid = this.ProvisioningJid,
				HttpFileUploadJid = this.HttpFileUploadJid,
				HttpFileUploadMaxSize = this.HttpFileUploadMaxSize,
				LogJid = this.LogJid,
				EDalerJid = this.EDalerJid,
				NeuroFeaturesJid = this.NeuroFeaturesJid,
				SupportsPushNotification = this.SupportsPushNotification,
				PinHash = this.PinHash,
				IsTest = this.IsTest,
				Purpose = this.Purpose,
				TestOtpTimestamp = this.TestOtpTimestamp,
				LegalIdentity = this.LegalIdentity,
				IdentityApplication = this.identityApplication,
				NrReviews = this.NrReviews,
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
				this.RegistryJid = configuration.RegistryJid;
				this.ProvisioningJid = configuration.ProvisioningJid;
				this.HttpFileUploadJid = configuration.HttpFileUploadJid;
				this.HttpFileUploadMaxSize = configuration.HttpFileUploadMaxSize;
				this.LogJid = configuration.LogJid;
				this.EDalerJid = configuration.EDalerJid;
				this.NeuroFeaturesJid = configuration.NeuroFeaturesJid;
				this.SupportsPushNotification = configuration.SupportsPushNotification;
				this.PinHash = configuration.PinHash;
				this.IsTest = configuration.IsTest;
				this.Purpose = configuration.Purpose;
				this.TestOtpTimestamp = configuration.TestOtpTimestamp;
				this.identityApplication = configuration.IdentityApplication;
				this.nrReviews = configuration.NrReviews;
				this.Theme = configuration.Theme;
				this.AuthenticationMethod = configuration.AuthenticationMethod;

				this.SetLegalIdentityInternal(configuration.LegalIdentity);

				this.SetTheme();
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
					string.IsNullOrWhiteSpace(this.registryJid) ||
					string.IsNullOrWhiteSpace(this.provisioningJid) ||
					string.IsNullOrWhiteSpace(this.httpFileUploadJid) ||
					string.IsNullOrWhiteSpace(this.logJid) ||
					string.IsNullOrWhiteSpace(this.eDalerJid) ||
					string.IsNullOrWhiteSpace(this.neuroFeaturesJid);
		}

		/// <summary>
		/// Returns <c>true</c> if the current <see cref="ITagProfile"/> needs to have its legal identity updated, <c>false</c> otherwise.
		/// </summary>
		/// <returns>If legal identity need updating</returns>
		public virtual bool LegalIdentityNeedsUpdating()
		{
			return this.legalIdentity?.IsDiscarded() ?? true;
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
			set
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
			set
			{
				if (!string.Equals(this.legalJid, value, StringComparison.Ordinal))
				{
					this.legalJid = value;
					this.FlagAsDirty(nameof(this.LegalJid));
				}
			}
		}

		/// <inheritdoc/>
		public string? RegistryJid
		{
			get => this.registryJid;
			set
			{
				if (!string.Equals(this.registryJid, value, StringComparison.Ordinal))
				{
					this.registryJid = value;
					this.FlagAsDirty(nameof(this.RegistryJid));
				}
			}
		}

		/// <inheritdoc/>
		public string? ProvisioningJid
		{
			get => this.provisioningJid;
			set
			{
				if (!string.Equals(this.provisioningJid, value, StringComparison.Ordinal))
				{
					this.provisioningJid = value;
					this.FlagAsDirty(nameof(this.ProvisioningJid));
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
			set
			{
				if (!string.Equals(this.logJid, value, StringComparison.Ordinal))
				{
					this.logJid = value;
					this.FlagAsDirty(nameof(this.LogJid));
				}
			}
		}

		/// <inheritdoc/>
		public string? EDalerJid
		{
			get => this.eDalerJid;
			set
			{
				if (!string.Equals(this.eDalerJid, value, StringComparison.Ordinal))
				{
					this.eDalerJid = value;
					this.FlagAsDirty(nameof(this.EDalerJid));
				}
			}
		}

		/// <inheritdoc/>
		public string? NeuroFeaturesJid
		{
			get => this.neuroFeaturesJid;
			set
			{
				if (!string.Equals(this.neuroFeaturesJid, value, StringComparison.Ordinal))
				{
					this.neuroFeaturesJid = value;
					this.FlagAsDirty(nameof(this.NeuroFeaturesJid));
				}
			}
		}

		/// <inheritdoc/>
		public bool SupportsPushNotification
		{
			get => this.supportsPushNotification;
			set
			{
				if (this.supportsPushNotification != value)
				{
					this.supportsPushNotification = value;
					this.FlagAsDirty(nameof(this.SupportsPushNotification));
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
			set
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
			set
			{
				if (this.testOtpTimestamp != value)
				{
					this.testOtpTimestamp = value;
					this.FlagAsDirty(nameof(this.TestOtpTimestamp));
				}
			}
		}

		/// <summary>
		/// The legal identity of the current user/profile.
		/// </summary>
		public LegalIdentity? LegalIdentity => this.legalIdentity;

		/// <summary>
		/// Sets the legal identity of the profile.
		/// </summary>
		/// <param name="Identity">Identity to set.</param>
		/// <param name="RemoveOldAttachments">If old attachments should be removed.</param>
		public async Task SetLegalIdentity(LegalIdentity? Identity, bool RemoveOldAttachments)
		{
			bool ScanIdUnlocked =
				!this.legalIdentity.HasApprovedPersonalInformation() &&
				Identity.HasApprovedPersonalInformation();

			Attachment[]? OldAttachments = this.SetLegalIdentityInternal(Identity);

			if (RemoveOldAttachments)
				await ServiceRef.AttachmentCacheService.RemoveAttachments(OldAttachments);

			if (ScanIdUnlocked)
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.Unlocked)],
						ServiceRef.Localizer[nameof(AppResources.YouCanNowScanIdCodes)]);
				});
			}
		}

		private Attachment[]? SetLegalIdentityInternal(LegalIdentity? Identity)
		{
			if (Equals(this.legalIdentity, Identity))
				return null;

			Attachment[]? OldAttachments = this.legalIdentity?.Attachments;
			bool RemoveAttachments = (this.legalIdentity is not null) && (Identity is null || this.legalIdentity.Id != Identity.Id);

			this.legalIdentity = Identity;
			this.FlagAsDirty(nameof(this.LegalIdentity));

			return RemoveAttachments ? OldAttachments : null;
		}

		/// <summary>
		/// Any current Identity application.
		/// </summary>
		public LegalIdentity? IdentityApplication => this.identityApplication;

		/// <summary>
		/// Sets the legal identity of the profile.
		/// </summary>
		/// <param name="Identity">Identity to set.</param>
		/// <param name="RemoveOldAttachments">If old attachments should be removed.</param>
		public async Task SetIdentityApplication(LegalIdentity? Identity, bool RemoveOldAttachments)
		{
			if (!Equals(this.identityApplication, Identity))
			{
				Attachment[]? OldAttachments = this.identityApplication?.Attachments;
				bool RemoveAttachments = (this.identityApplication is not null) && (Identity is null || this.identityApplication.Id != Identity.Id);

				this.identityApplication = Identity;
				this.FlagAsDirty(nameof(this.IdentityApplication));

				if (RemoveOldAttachments || Identity is null)
					this.NrReviews = 0;

				if (RemoveOldAttachments && RemoveAttachments)
					await ServiceRef.AttachmentCacheService.RemoveAttachments(OldAttachments);
			}
		}

		/// <summary>
		/// Number of peer reviews accepted for the current identity application.
		/// </summary>
		public int NrReviews
		{
			get => this.nrReviews;
			set
			{
				if (this.nrReviews != value)
				{
					this.nrReviews = value;
					this.FlagAsDirty(nameof(this.NrReviews));
				}
			}
		}

		/// <summary>
		/// Increments the number of peer reviews performed on the current identity application.
		/// </summary>
		public Task IncrementNrPeerReviews()
		{
			this.nrReviews++;
			this.FlagAsDirty(nameof(this.NrReviews));
			return Task.CompletedTask;
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
		public async Task SetAccountAndLegalIdentity(string AccountName, string ClientPasswordHash, string ClientPasswordHashMethod, LegalIdentity Identity)
		{
			this.PasswordHash = ClientPasswordHash;
			this.PasswordHashMethod = ClientPasswordHashMethod;
			this.ApiKey = string.Empty;
			this.ApiSecret = string.Empty;

			await this.SetLegalIdentity(Identity, true);

			// It's important for this to be the last, since it will fire the account change notification.
			this.Account = AccountName;
		}

		/// <summary>
		/// Revert the Set Account
		/// </summary>
		public void ClearAccount()
		{
			this.PasswordHash = string.Empty;
			this.PasswordHashMethod = string.Empty;
			this.LegalJid = string.Empty;
			this.RegistryJid = null;
			this.ProvisioningJid = null;
			this.EDalerJid = null;
			this.NeuroFeaturesJid = null;
			this.SupportsPushNotification = false;

			// It's important for this to be the last, since it will fire the account change notification.
			this.Account = string.Empty;
		}

		/// <summary>
		/// Revert the Set LegalIdentity
		/// </summary>
		public Task ClearLegalIdentity()
		{
			return this.SetLegalIdentity(null, true);
		}

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the revoked identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="RevokedIdentity">The revoked identity to use.</param>
		public Task RevokeLegalIdentity(LegalIdentity RevokedIdentity)
		{
			return this.SetLegalIdentity(RevokedIdentity, true);
		}

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the compromised identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="CompromisedIdentity">The compromised identity to use.</param>
		public Task CompromiseLegalIdentity(LegalIdentity CompromisedIdentity)
		{
			return this.SetLegalIdentity(CompromisedIdentity, true);
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

		/// <summary>
		/// Used during XMPP service discovery. Sets the file upload parameters.
		/// </summary>
		/// <param name="HttpFileUploadJid">The http file upload id.</param>
		/// <param name="MaxSize">The max size allowed.</param>
		public void SetFileUploadParameters(string HttpFileUploadJid, long MaxSize)
		{
			this.HttpFileUploadJid = HttpFileUploadJid;
			this.HttpFileUploadMaxSize = MaxSize;
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
			this.identityApplication = null;
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
			this.registryJid = string.Empty;
			this.provisioningJid = string.Empty;
			this.httpFileUploadJid = string.Empty;
			this.logJid = string.Empty;
			this.eDalerJid = string.Empty;
			this.neuroFeaturesJid = string.Empty;
			this.pinHash = string.Empty;
			this.httpFileUploadMaxSize = 0;
			this.isTest = false;
			this.TestOtpTimestamp = null;
			this.step = RegistrationStep.RequestPurpose;
			this.defaultXmppConnectivity = false;
			this.nrReviews = 0;
			this.supportsPushNotification = false;

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
					Constants.XmppProperties.MiddleNames,
					Constants.XmppProperties.LastNames,
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
