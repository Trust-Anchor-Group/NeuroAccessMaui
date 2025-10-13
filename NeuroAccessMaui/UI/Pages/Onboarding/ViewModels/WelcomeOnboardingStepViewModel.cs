using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.QR;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public partial class WelcomeOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private CancellationTokenSource? inviteCodeCts;
		private const int InviteCodeDebounceMs = 500;
		private bool autoAdvanced;

		public WelcomeOnboardingStepViewModel() : base(OnboardingStep.Welcome) { }

		[ObservableProperty]
		private bool isLoading = true;

		[ObservableProperty]
		private string? inviteCode;

		[ObservableProperty]
		private bool inviteCodeIsValid = true;

		public override string Title => ServiceRef.Localizer[nameof(AppResources.ActivateYourDigitalIdentity)];
		public override string NextButtonText => string.Empty; // Not used.
		public bool CanContinue => false; // Manual button disabled.

		internal override async Task OnActivatedAsync()
		{
			ServiceRef.LogService.LogInformational("Welcome step activated.");
			await base.OnActivatedAsync();
			if (this.IsLoading)
			{
				ServiceRef.LogService.LogDebug("Simulating initial loading delay.");
				await Task.Delay(800).ConfigureAwait(false);
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsLoading = false;
					ServiceRef.LogService.LogInformational("Welcome step loading complete.");
				});
			}
		}

		internal override Task<bool> OnNextAsync() => Task.FromResult(true);

		partial void OnInviteCodeChanged(string? value)
		{
			ServiceRef.LogService.LogDebug($"Invite code changed. RawValue='{value ?? string.Empty}' AutoAdvanced={this.autoAdvanced}.");
			if (this.autoAdvanced)
			{
				ServiceRef.LogService.LogDebug("Change ignored: already auto-advanced.");
				return;
			}

			this.inviteCodeCts?.Cancel();

			if (string.IsNullOrWhiteSpace(value))
			{
				this.InviteCodeIsValid = true;
				ServiceRef.LogService.LogDebug("Invite code empty -> awaiting input.");
				return;
			}

			bool IsValid = BasicValidateInviteCode(value, out string Trimmed);
			this.InviteCodeIsValid = IsValid;
			ServiceRef.LogService.LogInformational($"Invite code validation result: Valid={IsValid} Trimmed='{Trimmed}'.");
			if (!IsValid)
			{
				ServiceRef.LogService.LogWarning("Invite code failed basic validation.");
				return;
			}

			CancellationTokenSource DebounceCts = new CancellationTokenSource();
			this.inviteCodeCts = DebounceCts;
			ServiceRef.LogService.LogDebug($"Debounce scheduled ({InviteCodeDebounceMs} ms) for invitation processing.");

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(InviteCodeDebounceMs, DebounceCts.Token).ConfigureAwait(false);
					if (DebounceCts.IsCancellationRequested)
					{
						ServiceRef.LogService.LogDebug("Debounce cancelled before execution.");
						return;
					}
					if (!string.Equals(this.InviteCode, value, StringComparison.Ordinal))
					{
						ServiceRef.LogService.LogDebug("Invite code changed during debounce; aborting processing.");
						return;
					}
					ServiceRef.LogService.LogInformational("Debounce elapsed. Processing invitation.");
					await this.ProcessInvitationAsync(Trimmed).ConfigureAwait(false);
					MainThread.BeginInvokeOnMainThread(this.AdvanceAfterInvite);
				}
				catch (OperationCanceledException)
				{
					ServiceRef.LogService.LogDebug("Debounce task cancelled via token.");
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
				finally
				{
					DebounceCts.Dispose();
				}
			}, DebounceCts.Token);
		}

		private void AdvanceAfterInvite()
		{
			if (this.autoAdvanced)
			{
				ServiceRef.LogService.LogDebug("AdvanceAfterInvite ignored: already advanced.");
				return;
			}
			this.autoAdvanced = true;
			ServiceRef.LogService.LogInformational("Advancing to next onboarding step after invitation processing.");
			if (this.CoordinatorViewModel is not null)
				this.CoordinatorViewModel.GoToNextCommand.Execute(null);
		}

		[RelayCommand]
		private async Task PasteInviteCode()
		{
			ServiceRef.LogService.LogDebug("PasteInviteCode invoked.");
			if (!Clipboard.HasText)
			{
				ServiceRef.LogService.LogWarning("Clipboard has no text to paste.");
				return;
			}
			string? ClipboardText = await Clipboard.GetTextAsync();
			if (string.IsNullOrWhiteSpace(ClipboardText))
			{
				ServiceRef.LogService.LogWarning("Clipboard text is empty or whitespace.");
				return;
			}
			ServiceRef.LogService.LogInformational("Invite code pasted from clipboard.");
			await Clipboard.SetTextAsync(null);
			this.InviteCode = ClipboardText;
		}

		[RelayCommand]
		private async Task ScanQrCode()
		{
			ServiceRef.LogService.LogDebug("ScanQrCode command invoked.");
			string? Url = await QrCode.ScanQrCode(ServiceRef.Localizer[nameof(AppResources.QrPageTitleScanInvitation)], [Constants.UriSchemes.Onboarding]).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(Url))
			{
				ServiceRef.LogService.LogWarning("QR scan returned empty URL.");
				return;
			}
			string? Scheme = Constants.UriSchemes.GetScheme(Url);
			if (!string.Equals(Scheme, Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase))
			{
				ServiceRef.LogService.LogWarning($"QR scan scheme mismatch: '{Scheme}'.");
				return;
			}
			ServiceRef.LogService.LogInformational("Valid onboarding QR code scanned.");
			MainThread.BeginInvokeOnMainThread(() => this.InviteCode = Url);
		}

		internal override Task OnBackAsync()
		{
			ServiceRef.LogService.LogDebug("Back requested from Welcome step.");
			this.inviteCodeCts?.Cancel();
			return Task.CompletedTask;
		}

		private static bool BasicValidateInviteCode(string code, out string trimmed)
		{
			trimmed = code.Trim();
			if (string.IsNullOrEmpty(trimmed))
				return true; // Allow empty while typing
			if (!trimmed.StartsWith(Constants.UriSchemes.Onboarding + ":", StringComparison.OrdinalIgnoreCase))
				return false;
			string[] Parts = trimmed.Split(':', StringSplitOptions.RemoveEmptyEntries);
			if (Parts.Length != 5)
				return false;
			string Domain = Parts[1];
			string CodePart = Parts[2];
			string KeyPart = Parts[3];
			string IvPart = Parts[4];
			if (string.IsNullOrWhiteSpace(Domain) || Domain.Contains(' '))
				return false;
			if (string.IsNullOrWhiteSpace(CodePart))
				return false;
			try
			{
				_ = Convert.FromBase64String(KeyPart);
				_ = Convert.FromBase64String(IvPart);
			}
			catch { return false; }
			return true;
		}

		private async Task ProcessInvitationAsync(string inviteUrl)
		{
			ServiceRef.LogService.LogInformational($"Processing invitation: '{inviteUrl}'.");
			string[] Parts = inviteUrl.Split(':', StringSplitOptions.RemoveEmptyEntries);
			if (Parts.Length != 5)
			{
				ServiceRef.LogService.LogWarning("Invitation split length invalid.");
				return;
			}
			string Domain = Parts[1];
			string Code = Parts[2];
			string KeyStr = Parts[3];
			string IvStr = Parts[4];
			string EncryptedStr;
			Uri DomainInfoUri;
			try
			{
				DomainInfoUri = new Uri("https://" + Domain + "/Onboarding/GetInfo");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return;
			}
			try
			{
				ServiceRef.LogService.LogDebug("Posting invite code to domain info endpoint.");
				ContentBinaryResponse Response = await InternetContent.PostAsync(
					DomainInfoUri,
					Encoding.ASCII.GetBytes(Code),
					"text/plain",
					null,
					App.ValidateCertificateCallback,
					new KeyValuePair<string, string>("Accept", "text/plain"));
				Response.AssertOk();
				ContentResponse Decoded = await InternetContent.DecodeAsync(Response.ContentType, Response.Encoded, DomainInfoUri);
				Decoded.AssertOk();
				EncryptedStr = (string)Decoded.Decoded;
				ServiceRef.LogService.LogInformational("Invitation payload retrieved successfully.");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return;
			}

			XmlElement? LegalIdDefinition = null;
			bool AccountDone = false;
			try
			{
				ServiceRef.LogService.LogDebug("Decrypting invitation payload.");
				byte[] Key = Convert.FromBase64String(KeyStr);
				byte[] Iv = Convert.FromBase64String(IvStr);
				byte[] Encrypted = Convert.FromBase64String(EncryptedStr);
				using Aes Aes = Aes.Create();
				Aes.BlockSize = 128;
				Aes.KeySize = 256;
				Aes.Mode = CipherMode.CBC;
				Aes.Padding = PaddingMode.PKCS7;
				using ICryptoTransform Decryptor = Aes.CreateDecryptor(Key, Iv);
				byte[] Decrypted = Decryptor.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
				string XmlStr = Encoding.UTF8.GetString(Decrypted);
				XmlDocument Doc = new XmlDocument { PreserveWhitespace = true };
				Doc.LoadXml(XmlStr);
				if (Doc.DocumentElement is null || Doc.DocumentElement.NamespaceURI != ContractsClient.NamespaceOnboarding)
					throw new Exception("Invalid Invitation XML");
				ServiceRef.LogService.LogInformational("Invitation XML parsed.");
				LinkedList<XmlElement> ToProcess = new LinkedList<XmlElement>();
				ToProcess.AddLast(Doc.DocumentElement);
				while (ToProcess.First is not null)
				{
					XmlElement E = ToProcess.First.Value;
					ToProcess.RemoveFirst();
					ServiceRef.LogService.LogDebug($"Processing XML node '{E.LocalName}'.");
					switch (E.LocalName)
					{
						case "ApiKey":
							string ApiKey = XML.Attribute(E, "key");
							string Secret = XML.Attribute(E, "secret");
							string ApiDomain = XML.Attribute(E, "domain");
							ServiceRef.LogService.LogInformational($"Selecting domain (ApiKey) '{ApiDomain}'.");
							await SelectDomain(ApiDomain, ApiKey, Secret).ConfigureAwait(false);
							break;
						case "Account":
							string UserName = XML.Attribute(E, "userName");
							string Password = XML.Attribute(E, "password");
							string PasswordMethod = XML.Attribute(E, "passwordMethod");
							string AccDomain = XML.Attribute(E, "domain");
							ServiceRef.LogService.LogInformational($"Selecting domain (Account) '{AccDomain}'. Attempting auto-connect for user '{UserName}'.");
							await SelectDomain(AccDomain, string.Empty, string.Empty).ConfigureAwait(false);
							bool Connected = await this.ConnectToAccount(UserName, Password, PasswordMethod, string.Empty, LegalIdDefinition, string.Empty).ConfigureAwait(false);
							ServiceRef.LogService.LogInformational($"Auto-connect result: {(Connected ? "Succeeded" : "Failed")}.");
							if (Connected)
								AccountDone = true;
							break;
						case "LegalId":
							LegalIdDefinition = E;
							ServiceRef.LogService.LogInformational("LegalId definition captured.");
							break;
						case "Transfer":
							foreach (XmlNode N in E.ChildNodes)
								if (N is XmlElement E2)
									ToProcess.AddLast(E2);
							ServiceRef.LogService.LogDebug("Transfer node expanded.");
							break;
						default:
							ServiceRef.LogService.LogWarning($"Unexpected XML node '{E.LocalName}'.");
							throw new Exception("Invalid Invitation XML node");
					}
				}
				if (AccountDone && LegalIdDefinition is not null)
				{
					ServiceRef.LogService.LogInformational("Account and LegalId detected. Fast-forward advancing.");
					this.AdvanceAfterInvite();
					this.AdvanceAfterInvite();
				}
				else if (AccountDone)
				{
					ServiceRef.LogService.LogInformational("Account detected without LegalId. Single advance.");
					this.AdvanceAfterInvite();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private static async Task SelectDomain(string Domain, string Key, string Secret)
		{
			ServiceRef.LogService.LogDebug($"SelectDomain called for '{Domain}'.");
			bool DefaultConnectivity;
			try
			{
				(string HostName, int PortNumber, bool _) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(Domain);
				DefaultConnectivity = HostName == Domain && PortNumber == XmppCredentials.DefaultPort;
				ServiceRef.LogService.LogInformational($"Domain resolution: Host='{HostName}' Port={PortNumber} DefaultConnectivity={DefaultConnectivity}.");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				DefaultConnectivity = false;
			}
			ServiceRef.TagProfile.SetDomain(Domain, DefaultConnectivity, Key, Secret);
			ServiceRef.LogService.LogInformational("Domain selection stored in TagProfile.");
		}

		private async Task<bool> ConnectToAccount(string AccountName, string Password, string PasswordMethod, string LegalIdentityJid, XmlElement? LegalIdDefinition, string Pin)
		{
			ServiceRef.LogService.LogDebug($"ConnectToAccount attempt for '{AccountName}'. PasswordMethod='{PasswordMethod}'.");
			try
			{
				async Task OnConnected(XmppClient Client)
				{
					ServiceRef.LogService.LogInformational("XMPP account connected. Discovering identities.");
					DateTime Now = DateTime.Now;
					LegalIdentity? CreatedIdentity = null;
					LegalIdentity? ApprovedIdentity = null;
					bool ServiceDiscoverySucceeded = ServiceRef.TagProfile.NeedsUpdating() ? await ServiceRef.XmppService.DiscoverServices(Client) : true;
					ServiceRef.LogService.LogInformational($"Service discovery result: {ServiceDiscoverySucceeded}.");
					if (ServiceDiscoverySucceeded && !string.IsNullOrEmpty(ServiceRef.TagProfile.LegalJid))
					{
						bool DestroyContractsClient = false;
						if (!Client.TryGetExtension(typeof(ContractsClient), out IXmppExtension Extension) || Extension is not ContractsClient ContractsClient)
						{
							ContractsClient = new ContractsClient(Client, ServiceRef.TagProfile.LegalJid);
							DestroyContractsClient = true;
							ServiceRef.LogService.LogDebug("ContractsClient created for identity operations.");
						}
						try
						{
							if (LegalIdDefinition is not null)
							{
								ServiceRef.LogService.LogInformational("Importing provided LegalId keys.");
								await ContractsClient.ImportKeys(LegalIdDefinition);
							}
							LegalIdentity[] Identities = await ContractsClient.GetLegalIdentitiesAsync();
							ServiceRef.LogService.LogInformational($"Fetched {Identities.Length} identities.");
							foreach (LegalIdentity Identity in Identities)
							{
								try
								{
									if ((string.IsNullOrEmpty(LegalIdentityJid) || string.Compare(LegalIdentityJid, Identity.Id, StringComparison.OrdinalIgnoreCase) == 0) &&
										Identity.HasClientSignature && Identity.HasClientPublicKey && Identity.From <= Now && Identity.To >= Now &&
										(Identity.State == IdentityState.Approved || Identity.State == IdentityState.Created) && Identity.ValidateClientSignature() && await ContractsClient.HasPrivateKey(Identity))
									{
										if (Identity.State == IdentityState.Approved)
										{
											ApprovedIdentity = Identity;
											break;
										}
										CreatedIdentity ??= Identity;
									}
								}
								catch (Exception Ex2)
								{
									ServiceRef.LogService.LogException(Ex2);
								}
							}
							LegalIdentity? SelectedIdentity = ApprovedIdentity ?? CreatedIdentity;
							string SelectedId;
							if (SelectedIdentity is not null)
							{
								ServiceRef.LogService.LogInformational($"Selected identity '{SelectedIdentity.Id}' (State={SelectedIdentity.State}).");
								await ServiceRef.TagProfile.SetAccountAndLegalIdentity(AccountName, Client.PasswordHash, Client.PasswordHashMethod, SelectedIdentity);
								SelectedId = SelectedIdentity.Id;
								ServiceRef.TagProfile.SetXmppPasswordNeedsUpdating(true);
							}
							else
							{
								ServiceRef.LogService.LogInformational("No identity selected; storing account only.");
								ServiceRef.TagProfile.SetAccount(AccountName, Client.PasswordHash, Client.PasswordHashMethod);
								SelectedId = string.Empty;
							}
							if (!string.IsNullOrEmpty(Pin))
							{
								ServiceRef.LogService.LogInformational("Local PIN set from invitation.");
								ServiceRef.TagProfile.LocalPassword = Pin;
							}
							foreach (LegalIdentity Identity in Identities)
							{
								if (Identity.Id == SelectedId)
									continue;
								switch (Identity.State)
								{
									case IdentityState.Approved:
									case IdentityState.Created:
										ServiceRef.LogService.LogDebug($"Obsoleting identity '{Identity.Id}'.");
										await ContractsClient.ObsoleteLegalIdentityAsync(Identity.Id);
										break;
								}
							}
						}
						finally
						{
							if (DestroyContractsClient)
							{
								ServiceRef.LogService.LogDebug("Disposing temporary ContractsClient.");
								ContractsClient.Dispose();
							}
						}
					}
				}
				(string HostName, int PortNumber, bool IsIp) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(ServiceRef.TagProfile.Domain ?? string.Empty);
				ServiceRef.LogService.LogInformational($"Connecting to XMPP account Host='{HostName}' Port={PortNumber} IsIp={IsIp}.");
				(bool Succeeded, string? ErrorMessage, string[]? _) = await ServiceRef.XmppService.TryConnectAndConnectToAccount(ServiceRef.TagProfile.Domain ?? string.Empty, IsIp, HostName, PortNumber, AccountName, Password, PasswordMethod, Constants.LanguageCodes.Default, typeof(App).Assembly, OnConnected);
				ServiceRef.LogService.LogInformational($"XMPP connect result: {(Succeeded ? "Succeeded" : "Failed")} Error='{ErrorMessage}'.");
				if (!Succeeded && !string.IsNullOrEmpty(ErrorMessage))
					ServiceRef.LogService.LogWarning(ErrorMessage);
				return Succeeded;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			return false;
		}
	}
}
