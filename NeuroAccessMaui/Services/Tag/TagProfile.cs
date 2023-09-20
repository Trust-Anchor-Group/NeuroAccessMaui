using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.EventLog;
using System.Text;
using System.Text.RegularExpressions;
using Waher.Networking;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace NeuroAccessMaui.Services.Tag;

/// <summary>
/// The different steps of a TAG Profile registration journey.
/// </summary>
public enum RegistrationStep
{
	/// <summary>
	/// Validate Phone Number and e-mail address
	/// </summary>
	ValidateContactInfo = 0,

	/// <summary>
	/// Create or connect to an account
	/// </summary>
	Account = 1,

	/// <summary>
	/// Register an identity
	/// </summary>
	RegisterIdentity = 2,

	/// <summary>
	/// Have the identity validated.
	/// </summary>
	ValidateIdentity = 3,

	/// <summary>
	/// Create a PIN code
	/// </summary>
	Pin = 4,

	/// <summary>
	/// Profile is completed.
	/// </summary>
	Complete = 5
}

/// <summary>
/// For what purpose the app will be used
/// </summary>
public enum PurposeUse
{
	/// <summary>
	/// For personal use
	/// </summary>
	Personal = 0,

	/// <summary>
	/// For use at work
	/// </summary>
	Work = 1,

	/// <summary>
	/// For educational use
	/// </summary>
	Educational = 2,

	/// <summary>
	/// For experimental use
	/// </summary>
	Experimental = 3
}

/// <inheritdoc/>
[Singleton]
public class TagProfile : ITagProfile
{
	/// <summary>
	/// An event that fires every time the <see cref="Step"/> property changes.
	/// </summary>
	public event EventHandlerAsync StepChanged;

	/// <summary>
	/// An event that fires every time any property changes.
	/// </summary>
	public event System.ComponentModel.PropertyChangedEventHandler Changed;

	private LegalIdentity legalIdentity;
	private string objectId;
	private string domain;
	private string apiKey;
	private string apiSecret;
	private string phoneNumber;
	private string eMail;
	private string account;
	private string passwordHash;
	private string passwordHashMethod;
	private string legalJid;
	private string httpFileUploadJid;
	private string logJid;
	private string mucJid;
	private string pinHash;
	private long? httpFileUploadMaxSize;
	private bool isTest;
	private PurposeUse purpose;
	private DateTime? testOtpTimestamp;
	private RegistrationStep step = RegistrationStep.ValidateContactInfo;
	private bool suppressPropertyChangedEvents;
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
	protected virtual async Task OnStepChanged(EventArgs e)
	{
		try
		{
			Task T = StepChanged?.Invoke(this, EventArgs.Empty);
			if (T is not null)
				await T;
		}
		catch (Exception ex)
		{
			ILogService LogService = App.Instantiate<ILogService>();
			LogService.LogException(ex);
		}
	}

	/// <summary>
	/// Invoked whenever any property changes, to fire the <see cref="Changed"/> event.
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnChanged(System.ComponentModel.PropertyChangedEventArgs e)
	{
		Changed?.Invoke(this, e);
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
			Domain = this.Domain,
			ApiKey = this.ApiKey,
			ApiSecret = this.ApiSecret,
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
			Step = this.Step
		};

		return Clone;
	}

	/// <summary>
	/// Parses an instance of a <see cref="TagConfiguration"/> object to update this instance's properties.
	/// </summary>
	/// <param name="configuration"></param>
	public async Task FromConfiguration(TagConfiguration configuration)
	{
		try
		{
			this.suppressPropertyChangedEvents = true;

			this.objectId = configuration.ObjectId;
			this.Domain = configuration.Domain;
			this.ApiKey = configuration.ApiKey;
			this.ApiSecret = configuration.ApiSecret;
			this.PhoneNumber = configuration.PhoneNumber;
			this.EMail = configuration.EMail;
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

			// Do this last, as listeners will read the other properties when the event is fired.
			await this.SetStep(configuration.Step);
		}
		finally
		{
			this.suppressPropertyChangedEvents = false;
		}
	}

	/// <inheritdoc/>
	public virtual bool NeedsUpdating()
	{
		return string.IsNullOrWhiteSpace(this.legalJid) ||
			   string.IsNullOrWhiteSpace(this.httpFileUploadJid) ||
			   string.IsNullOrWhiteSpace(this.logJid) ||
			   string.IsNullOrWhiteSpace(this.mucJid);
	}

	/// <inheritdoc/>
	public virtual bool LegalIdentityNeedsUpdating()
	{
		return this.legalIdentity.NeedsUpdating();
	}

	/// <inheritdoc/>
	public virtual bool IsCompleteOrWaitingForValidation()
	{
		return this.Step >= RegistrationStep.ValidateIdentity;
	}

	/// <inheritdoc/>
	public virtual bool IsComplete()
	{
		return this.Step == RegistrationStep.Complete;
	}

	#region Properties

	/// <inheritdoc/>
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

	/// <inheritdoc/>
	public string Domain
	{
		get => this.domain;
		private set
		{
			if (!string.Equals(this.domain, value))
			{
				this.domain = value;
				this.FlagAsDirty(nameof(this.Domain));
			}
		}
	}

	/// <inheritdoc/>
	public string ApiKey
	{
		get => this.apiKey;
		private set
		{
			if (!string.Equals(this.apiKey, value))
			{
				this.apiKey = value;
				this.FlagAsDirty(nameof(this.ApiKey));
			}
		}
	}

	/// <inheritdoc/>
	public string ApiSecret
	{
		get => this.apiSecret;
		private set
		{
			if (!string.Equals(this.apiSecret, value))
			{
				this.apiSecret = value;
				this.FlagAsDirty(nameof(this.ApiSecret));
			}
		}
	}

	/// <inheritdoc/>
	public string PhoneNumber
	{
		get => this.phoneNumber;
		private set
		{
			if (!string.Equals(this.phoneNumber, value))
			{
				this.phoneNumber = value;
				this.FlagAsDirty(nameof(this.PhoneNumber));
			}
		}
	}

	/// <inheritdoc/>
	public string EMail
	{
		get => this.eMail;
		private set
		{
			if (!string.Equals(this.eMail, value))
			{
				this.eMail = value;
				this.FlagAsDirty(nameof(this.EMail));
			}
		}
	}

	/// <inheritdoc/>
	public string Account
	{
		get => this.account;
		private set
		{
			if (!string.Equals(this.account, value))
			{
				this.account = value;
				this.FlagAsDirty(nameof(this.Account));
			}
		}
	}

	/// <inheritdoc/>
	public string PasswordHash
	{
		get => this.passwordHash;
		private set
		{
			if (!string.Equals(this.passwordHash, value))
			{
				this.passwordHash = value;
				this.FlagAsDirty(nameof(this.PasswordHash));
			}
		}
	}

	/// <inheritdoc/>
	public string PasswordHashMethod
	{
		get => this.passwordHashMethod;
		private set
		{
			if (!string.Equals(this.passwordHashMethod, value))
			{
				this.passwordHashMethod = value;
				this.FlagAsDirty(nameof(this.PasswordHashMethod));
			}
		}
	}

	/// <inheritdoc/>
	public string LegalJid
	{
		get => this.legalJid;
		private set
		{
			if (!string.Equals(this.legalJid, value))
			{
				this.legalJid = value;
				this.FlagAsDirty(nameof(this.LegalJid));
			}
		}
	}

	/// <inheritdoc/>
	public string HttpFileUploadJid
	{
		get => this.httpFileUploadJid;
		private set
		{
			if (!string.Equals(this.httpFileUploadJid, value))
			{
				this.httpFileUploadJid = value;
				this.FlagAsDirty(nameof(this.HttpFileUploadJid));
			}
		}
	}

	/// <inheritdoc/>
	public long? HttpFileUploadMaxSize
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

	/// <inheritdoc/>
	public string LogJid
	{
		get => this.logJid;
		private set
		{
			if (!string.Equals(this.logJid, value))
			{
				this.logJid = value;
				this.FlagAsDirty(nameof(this.LogJid));
			}
		}
	}

	/// <inheritdoc/>
	public RegistrationStep Step => this.step;

	private async Task SetStep(RegistrationStep NewStep)
	{
		if (this.step != NewStep)
		{
			this.step = NewStep;
			this.FlagAsDirty(nameof(this.Step));
			await this.OnStepChanged(EventArgs.Empty);
		}
	}

	/// <inheritdoc/>
	public bool FileUploadIsSupported => !string.IsNullOrWhiteSpace(this.HttpFileUploadJid) && this.HttpFileUploadMaxSize.HasValue;

	/// <inheritdoc/>
	public string Pin
	{
		set => this.PinHash = this.ComputePinHash(value);
	}

	/// <inheritdoc/>
	public string PinHash
	{
		get => this.pinHash;
		private set
		{
			if (!string.Equals(this.pinHash, value))
			{
				this.pinHash = value;
				this.FlagAsDirty(nameof(this.PinHash));
			}
		}
	}

	/// <inheritdoc/>
	public bool HasPin
	{
		get => !string.IsNullOrEmpty(this.PinHash);
	}

	/// <inheritdoc/>
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

	/// <inheritdoc/>
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

	/// <inheritdoc/>
	public LegalIdentity LegalIdentity
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

	/// <inheritdoc/>
	public bool IsDirty { get; private set; }

	private void FlagAsDirty(string propertyName)
	{
		this.IsDirty = true;

		if (!this.suppressPropertyChangedEvents)
			this.OnChanged(new System.ComponentModel.PropertyChangedEventArgs(propertyName));
	}

	/// <inheritdoc/>
	public void ResetIsDirty()
	{
		this.IsDirty = false;
	}

	#endregion

	#region Build Steps

	private async Task DecrementConfigurationStep(RegistrationStep? stepToRevertTo = null)
	{
		if (stepToRevertTo.HasValue)
			await this.SetStep(stepToRevertTo.Value);
		else
		{
			switch (this.Step)
			{
				case RegistrationStep.ValidateContactInfo:
					// Do nothing
					break;

				case RegistrationStep.Account:
					await this.SetStep(RegistrationStep.ValidateContactInfo);
					break;

				case RegistrationStep.RegisterIdentity:
					await this.SetStep(RegistrationStep.ValidateContactInfo);
					break;

				case RegistrationStep.ValidateIdentity:
					await this.SetStep(RegistrationStep.RegisterIdentity);
					break;

				case RegistrationStep.Pin:
					await this.SetStep(RegistrationStep.ValidateIdentity);
					break;
			}
		}
	}

	private async Task IncrementConfigurationStep(RegistrationStep? stepToGoTo = null)
	{
		if (stepToGoTo.HasValue)
			await this.SetStep(stepToGoTo.Value);
		else
		{
			switch (this.Step)
			{
				case RegistrationStep.ValidateContactInfo:
					await this.SetStep(this.LegalIdentity is null ? RegistrationStep.Account : RegistrationStep.RegisterIdentity);
					break;

				case RegistrationStep.Account:
					await this.SetStep(RegistrationStep.RegisterIdentity);
					break;

				case RegistrationStep.RegisterIdentity:
					await this.SetStep(RegistrationStep.ValidateIdentity);
					break;

				case RegistrationStep.ValidateIdentity:
					await this.SetStep(RegistrationStep.Pin);
					break;

				case RegistrationStep.Pin:
					await this.SetStep(RegistrationStep.Complete);
					break;
			}
		}
	}

	/// <inheritdoc/>
	public void SetPhone(string PhoneNumber)
	{
		this.PhoneNumber = PhoneNumber;
	}

	/// <inheritdoc/>
	public void SetEMail(string EMail)
	{
		this.EMail = EMail;
	}

	/// <inheritdoc/>
	public async Task SetDomain(string domainName, bool defaultXmppConnectivity, string Key, string Secret)
	{
		this.Domain = domainName;
		this.DefaultXmppConnectivity = defaultXmppConnectivity;
		this.ApiKey = Key;
		this.ApiSecret = Secret;

		if (!string.IsNullOrWhiteSpace(this.Domain) && this.Step == RegistrationStep.ValidateContactInfo)
			await this.IncrementConfigurationStep();
	}

	/// <inheritdoc/>
	public async Task ClearDomain()
	{
		this.Domain = string.Empty;
		await this.DecrementConfigurationStep(RegistrationStep.ValidateContactInfo);
	}

	/// <inheritdoc/>
	public async Task RevalidateContactInfo()
	{
		if (!string.IsNullOrWhiteSpace(this.Domain) && this.Step == RegistrationStep.ValidateContactInfo)
			await this.IncrementConfigurationStep();
	}

	/// <inheritdoc/>
	public async Task InvalidateContactInfo()
	{
		await this.DecrementConfigurationStep();
	}

	/// <inheritdoc/>
	public async Task SetAccount(string accountName, string clientPasswordHash, string clientPasswordHashMethod)
	{
		this.Account = accountName;
		this.PasswordHash = clientPasswordHash;
		this.PasswordHashMethod = clientPasswordHashMethod;
		this.ApiKey = string.Empty;
		this.ApiSecret = string.Empty;

		if (!string.IsNullOrWhiteSpace(this.Account) && this.Step == RegistrationStep.Account)
			await this.IncrementConfigurationStep();
	}

	/// <inheritdoc/>
	public async Task SetAccountAndLegalIdentity(string accountName, string clientPasswordHash, string clientPasswordHashMethod, LegalIdentity identity)
	{
		this.Account = accountName;
		this.PasswordHash = clientPasswordHash;
		this.PasswordHashMethod = clientPasswordHashMethod;
		this.LegalIdentity = identity;
		this.ApiKey = string.Empty;
		this.ApiSecret = string.Empty;

		if (!string.IsNullOrWhiteSpace(this.Account) && this.Step == RegistrationStep.Account && this.LegalIdentity is not null)
		{
			switch (this.LegalIdentity.State)
			{
				case IdentityState.Created:
					await this.IncrementConfigurationStep(RegistrationStep.ValidateIdentity);
					break;

				case IdentityState.Approved:
					await this.IncrementConfigurationStep(
						this.HasPin ? RegistrationStep.Complete : RegistrationStep.Pin);
					break;

				default:
					await this.IncrementConfigurationStep();
					break;
			}
		}
	}

	/// <inheritdoc/>
	public async Task ClearAccount(bool GoToPrevStep = true)
	{
		this.Account = string.Empty;
		this.PasswordHash = string.Empty;
		this.PasswordHashMethod = string.Empty;
		this.LegalJid = null;

		if (GoToPrevStep)
		{
			await this.DecrementConfigurationStep(RegistrationStep.ValidateContactInfo);
		}
	}

	/// <inheritdoc/>
	public async Task SetLegalIdentity(LegalIdentity Identity)
	{
		this.LegalIdentity = Identity;

		if (this.Step == RegistrationStep.RegisterIdentity && Identity is not null &&
			(Identity.State == IdentityState.Created || Identity.State == IdentityState.Approved))
		{
			await this.IncrementConfigurationStep();
		}
	}

	/// <inheritdoc/>
	public Task ClearLegalIdentity()
	{
		this.LegalIdentity = null;
		this.LegalJid = null;

		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public async Task RevokeLegalIdentity(LegalIdentity revokedIdentity)
	{
		this.LegalIdentity = revokedIdentity;
		await this.DecrementConfigurationStep(RegistrationStep.ValidateContactInfo);
	}

	/// <inheritdoc/>
	public async Task CompromiseLegalIdentity(LegalIdentity compromisedIdentity)
	{
		this.LegalIdentity = compromisedIdentity;
		await this.DecrementConfigurationStep(RegistrationStep.ValidateContactInfo);
	}

	/// <inheritdoc/>
	public async Task SetIsValidated()
	{
		if (this.Step == RegistrationStep.ValidateIdentity)
			await this.IncrementConfigurationStep();
	}

	/// <inheritdoc/>
	public async Task ClearIsValidated()
	{
		await this.DecrementConfigurationStep(RegistrationStep.RegisterIdentity);
	}

	/// <inheritdoc/>
	public async Task CompletePinStep(string Pin, bool AddOrUpdatePin = true)
	{
		if (AddOrUpdatePin)
		{
			this.Pin = Pin;
		}

		if (this.step == RegistrationStep.Pin)
			await this.IncrementConfigurationStep();
	}

	/// <inheritdoc/>
	public async Task RevertPinStep()
	{
		if (this.Step == RegistrationStep.Pin)
			await this.DecrementConfigurationStep(RegistrationStep.ValidateIdentity); // prev
	}

	/// <inheritdoc/>
	public void SetPurpose(bool IsTest, PurposeUse Purpose)
	{
		this.IsTest = IsTest;
		this.Purpose = Purpose;
	}

	/// <inheritdoc/>
	public void SetTestOtpTimestamp(DateTime? timestamp)
	{
		this.TestOtpTimestamp = timestamp;
	}

	/// <inheritdoc/>
	public void SetLegalJid(string legalJid)
	{
		this.LegalJid = legalJid;
	}

	/// <inheritdoc/>
	public void SetFileUploadParameters(string httpFileUploadJid, long? maxSize)
	{
		this.HttpFileUploadJid = httpFileUploadJid;
		this.HttpFileUploadMaxSize = maxSize;
	}

	/// <inheritdoc/>
	public void SetLogJid(string logJid)
	{
		this.LogJid = logJid;
	}

	#endregion

	/// <inheritdoc/>
	public string ComputePinHash(string pin)
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
		sb.Append(pin);

		string s = sb.ToString();
		byte[] data = Encoding.UTF8.GetBytes(s);

		return Hashes.ComputeSHA384HashString(data);
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
		this.httpFileUploadMaxSize = null;
		this.isTest = false;
		this.TestOtpTimestamp = null;
		this.step = RegistrationStep.ValidateContactInfo;
		this.defaultXmppConnectivity = false;

		this.IsDirty = true;
	}

	/// <inheritdoc/>
	public PinStrength ValidatePinStrength(string Pin)
	{
		if (Pin is null)
			return PinStrength.NotEnoughDigitsLettersSigns;

		Pin = Pin.Normalize();

		int DigitsCount = 0;
		int LettersCount = 0;
		int SignsCount = 0;

		Dictionary<int, int> DistinctSymbolsCount = new();

		int[] SlidingWindow = new int[Constants.Authentication.MaxPinSequencedSymbols + 1];
		SlidingWindow.Initialize();

		for (int i = 0; i < Pin.Length;)
		{
			if (char.IsDigit(Pin, i))
			{
				DigitsCount++;
			}
			else if (char.IsLetter(Pin, i))
			{
				LettersCount++;
			}
			else
			{
				SignsCount++;
			}

			int Symbol = char.ConvertToUtf32(Pin, i);

			if (DistinctSymbolsCount.TryGetValue(Symbol, out int SymbolCount))
			{
				DistinctSymbolsCount[Symbol] = ++SymbolCount;
				if (SymbolCount > Constants.Authentication.MaxPinIdenticalSymbols)
				{
					return PinStrength.TooManyIdenticalSymbols;
				}
			}
			else
			{
				DistinctSymbolsCount.Add(Symbol, 1);
			}

			for (int j = 0; j < SlidingWindow.Length - 1; j++)
				SlidingWindow[j] = SlidingWindow[j + 1];
			SlidingWindow[^1] = Symbol;

			int[] SlidingWindowDifferences = new int[SlidingWindow.Length - 1];
			for (int j = 0; j < SlidingWindow.Length - 1; j++)
			{
				SlidingWindowDifferences[j] = SlidingWindow[j + 1] - SlidingWindow[j];
			}

			if (SlidingWindowDifferences.All(difference => difference == 1))
			{
				return PinStrength.TooManySequencedSymbols;
			}

			if (char.IsSurrogate(Pin, i))
			{
				i += 2;
			}
			else
			{
				i += 1;
			}
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
			.SelectMany(PropertyKey => LegalIdentity[PropertyKey] is string PropertyValue ? Regex.Split(PropertyValue, @"\p{Zs}+") : Enumerable.Empty<string>())
			.Where(Word => Word?.GetUnicodeLength() > 2);

			if (NameWords.Any(NameWord => Pin.Contains(NameWord, Comparison)))
				return PinStrength.ContainsName;

			IEnumerable<string> AddressWords = new string[]
			{
				Constants.XmppProperties.Address,
				Constants.XmppProperties.Address2,
			}
			.SelectMany(PropertyKey => LegalIdentity[PropertyKey] is string PropertyValue ? Regex.Split(PropertyValue, @"\p{Zs}+") : Enumerable.Empty<string>())
			.Where(Word => Word?.GetUnicodeLength() > 2);

			if (AddressWords.Any(AddressWord => Pin.Contains(AddressWord, Comparison)))
				return PinStrength.ContainsAddress;
		}

		const int MinDigitsCount = Constants.Authentication.MinPinSymbolsFromDifferentClasses;
		const int MinLettersCount = Constants.Authentication.MinPinSymbolsFromDifferentClasses;
		const int MinSignsCount = Constants.Authentication.MinPinSymbolsFromDifferentClasses;

		if (DigitsCount < MinDigitsCount && LettersCount < MinLettersCount && SignsCount < MinSignsCount)
		{
			return PinStrength.NotEnoughDigitsLettersSigns;
		}

		if (DigitsCount >= MinDigitsCount && LettersCount < MinLettersCount && SignsCount < MinSignsCount)
		{
			return PinStrength.NotEnoughLettersOrSigns;
		}

		if (DigitsCount < MinDigitsCount && LettersCount >= MinLettersCount && SignsCount < MinSignsCount)
		{
			return PinStrength.NotEnoughDigitsOrSigns;
		}

		if (DigitsCount < MinDigitsCount && LettersCount < MinLettersCount && SignsCount >= MinSignsCount)
		{
			return PinStrength.NotEnoughLettersOrDigits;
		}

		if (DigitsCount + LettersCount + SignsCount < Constants.Authentication.MinPinLength)
		{
			return PinStrength.TooShort;
		}

		return PinStrength.Strong;
	}
}
