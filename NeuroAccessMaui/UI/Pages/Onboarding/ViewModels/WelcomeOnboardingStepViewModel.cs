using System;
using System.Collections.Generic;
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
using NeuroAccessMaui.UI.Pages.Onboarding;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Provides state and logic for the welcome onboarding step, including invite and QR handling.
	/// </summary>
	public partial class WelcomeOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private CancellationTokenSource? inviteCodeCts;
		private const int InviteCodeDebounceMs = 500;
		private bool autoAdvanced;

		public WelcomeOnboardingStepViewModel() : base(OnboardingStep.Welcome) { }

		/// <summary>
		/// Gets or sets a value indicating whether the welcome content is in its initial loading state.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsBusy))]
		private bool isLoading = true;

		/// <summary>
		/// Gets or sets the invite code entered or scanned by the user.
		/// </summary>
		[ObservableProperty]
		private string? inviteCode;

		/// <summary>
		/// Gets or sets a value indicating whether the current invite code passes basic validation.
		/// </summary>
		[ObservableProperty]
		private bool inviteCodeIsValid = true;

		/// <summary>
		/// Gets or sets a value indicating whether an invite payload is currently being processed.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsBusy))]
		private bool isProcessingInvite;

		/// <summary>
		/// Gets the localized title displayed on the welcome step.
		/// </summary>
		public override string Title => ServiceRef.Localizer[nameof(AppResources.ActivateYourDigitalIdentity)];

		/// <summary>
		/// Gets the text displayed on the (unused) next button.
		/// </summary>
		public override string NextButtonText => string.Empty; // Not used.

		/// <summary>
		/// Gets a value indicating whether manual continuation is allowed.
		/// </summary>
		public bool CanContinue => false; // Manual button disabled.

		/// <summary>
		/// Gets a value indicating whether the welcome step is initializing or processing invite data.
		/// </summary>
		public bool IsBusy => this.IsLoading || this.IsProcessingInvite;

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

			this.CancelPendingInviteProcessing();

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
					this.SetInviteProcessingState(true);
					try
					{
						bool Processed = await this.ProcessInvitationAsync(Trimmed).ConfigureAwait(false);
						if (Processed)
						{
							MainThread.BeginInvokeOnMainThread(this.AdvanceAfterInvite);
						}
					}
					finally
					{
						this.SetInviteProcessingState(false);
					}
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
					if (ReferenceEquals(this.inviteCodeCts, DebounceCts))
					{
						this.inviteCodeCts = null;
					}

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

		[RelayCommand]
		private async Task SelectForMe()
		{
			ServiceRef.LogService.LogInformational("SelectForMe command invoked from Welcome step.");

			this.CancelPendingInviteProcessing();

			if (!await this.PrepareAutomaticProviderSelectionAsync().ConfigureAwait(false))
			{
				ServiceRef.LogService.LogWarning("Automatic provider preparation failed. Staying on Welcome step.");
				return;
			}

			bool NavigationSucceeded = false;
			try
			{
				this.autoAdvanced = true;

				await MainThread.InvokeOnMainThreadAsync(() => this.InviteCode = null);

				if (this.CoordinatorViewModel is null)
				{
					ServiceRef.LogService.LogWarning("Coordinator not attached; cannot advance to ValidatePhone.");
					return;
				}

				ServiceRef.LogService.LogInformational("Advancing directly to ValidatePhone step after automatic provider selection.");
				await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.ValidatePhone).ConfigureAwait(false);
				NavigationSucceeded = true;
			}
			finally
			{
				if (!NavigationSucceeded)
				{
					this.autoAdvanced = false;
				}
			}
		}

		private async Task<bool> PrepareAutomaticProviderSelectionAsync()
		{
			try
			{
				bool HadExistingDomain = !string.IsNullOrEmpty(ServiceRef.TagProfile.Domain) ||
					!string.IsNullOrEmpty(ServiceRef.TagProfile.ApiKey) ||
					!string.IsNullOrEmpty(ServiceRef.TagProfile.ApiSecret);

				if (HadExistingDomain)
				{
					ServiceRef.LogService.LogInformational("Clearing previously selected domain to enable automatic provider selection.");
				}
				else
				{
					ServiceRef.LogService.LogInformational("No domain selected; ready for automatic provider selection.");
				}

				ServiceRef.TagProfile.ClearDomain();
				return true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				});

				return false;
			}
		}

		internal override Task OnBackAsync()
		{
			ServiceRef.LogService.LogDebug("Back requested from Welcome step.");
			this.CancelPendingInviteProcessing();
			return Task.CompletedTask;
		}

		private void SetInviteProcessingState(bool isProcessing)
		{
			if (MainThread.IsMainThread)
			{
				this.IsProcessingInvite = isProcessing;
			}
			else
			{
				MainThread.BeginInvokeOnMainThread(() => this.IsProcessingInvite = isProcessing);
			}
		}

		private void CancelPendingInviteProcessing()
		{
			CancellationTokenSource? existing = Interlocked.Exchange(ref this.inviteCodeCts, null);
			if (existing is null)
			{
				return;
			}

			try
			{
				existing.Cancel();
			}
			catch (ObjectDisposedException)
			{
				// Already disposed.
			}
		}

		private void MarkInviteAsInvalid(string messageResourceKey)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				this.InviteCodeIsValid = false;
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[messageResourceKey],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			});
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

		private async Task<bool> ProcessInvitationAsync(string inviteUrl)
		{
			ServiceRef.LogService.LogInformational($"Processing invitation: '{inviteUrl}'.");
			string[] Parts = inviteUrl.Split(':', StringSplitOptions.RemoveEmptyEntries);
			if (Parts.Length != 5)
			{
				ServiceRef.LogService.LogWarning("Invitation split length invalid.");
				this.MarkInviteAsInvalid(nameof(AppResources.InvalidInvitationCode));
				return false;
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
				this.MarkInviteAsInvalid(nameof(AppResources.InvalidInvitationCode));
				return false;
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
				this.MarkInviteAsInvalid(nameof(AppResources.UnableToAccessInvitation));
				return false;
			}

			XmlElement? LegalIdDefinition = null;
			string? transferredAccount = null;
			string? transferredPassword = null;
			string? transferredPasswordMethod = null;
			bool domainSelectedFromInvite = false;
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
							domainSelectedFromInvite = true;
							break;
						case "Account":
							string UserName = XML.Attribute(E, "userName");
							string Password = XML.Attribute(E, "password");
							string PasswordMethod = XML.Attribute(E, "passwordMethod");
							string AccDomain = XML.Attribute(E, "domain");
							ServiceRef.LogService.LogInformational($"Selecting domain (Account) '{AccDomain}'. Capturing transfer for user '{UserName}'.");
							await SelectDomain(AccDomain, string.Empty, string.Empty).ConfigureAwait(false);
							transferredAccount = UserName;
							transferredPassword = Password;
							transferredPasswordMethod = PasswordMethod;
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
							ServiceRef.LogService.LogWarning($"Unexpected XML node '<{E.LocalName}>'.");
							break;
					}
				}

				bool advancedManually = false;
				bool shouldAdvance = false;
				if (!string.IsNullOrEmpty(transferredAccount) && transferredPassword is not null && transferredPasswordMethod is not null && this.CoordinatorViewModel is not null)
				{
					OnboardingTransferContext Context = new OnboardingTransferContext(transferredAccount, transferredPassword, transferredPasswordMethod, LegalIdDefinition);
					await this.CoordinatorViewModel.ApplyTransferContextAsync(Context).ConfigureAwait(false);
					shouldAdvance = true;

					bool Connected = await this.CoordinatorViewModel.TryFinalizeTransferAsync(false).ConfigureAwait(false);
					ServiceRef.LogService.LogInformational("Deferred transfer connection attempt finished.",
						new KeyValuePair<string, object?>("Success", Connected),
						new KeyValuePair<string, object?>("HasLegalId", Context.HasLegalIdentity));
					if (Connected && Context.HasLegalIdentity)
					{
						advancedManually = true;
						MainThread.BeginInvokeOnMainThread(async () =>
						{
							await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.DefinePassword);
						});
					}
				}
				else
				{
					if (domainSelectedFromInvite)
					{
						shouldAdvance = true;
					}
					else
					{
						this.MarkInviteAsInvalid(nameof(AppResources.InvalidInvitationCode));
						return false;
					}
				}

				if (!advancedManually)
				{
					return shouldAdvance;
				}

				return false;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.MarkInviteAsInvalid(nameof(AppResources.InvalidInvitationCode));
			}

			return false;
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

	}
}
