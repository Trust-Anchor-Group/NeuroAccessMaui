using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui;
using NeuroAccessMaui.Links;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.UI.Pages.Onboarding;
using Waher.Content.Xml;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Provides state and logic for the welcome onboarding step, including invite and QR handling.
	/// </summary>
	/// <remarks>
	/// Invitation codes contain the key that decrypts the invitation, so neither the code nor the invitation contents are logged.
	/// </remarks>
	public partial class WelcomeOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private const int inviteCodeDebounceMs = 500;

		private readonly IQrService qrService;
		private readonly ILinkService linkService;
		private CancellationTokenSource? inviteCodeCts;
		private bool autoAdvanced;

		/// <summary>
		/// Initializes a new instance of the <see cref="WelcomeOnboardingStepViewModel"/> class using the registered QR and link services.
		/// </summary>
		public WelcomeOnboardingStepViewModel()
			: this(QrCode.GetQrService(), QrCode.GetLinkService())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WelcomeOnboardingStepViewModel"/> class.
		/// </summary>
		/// <param name="QrService">Scans invitation codes and prepares them before the scanner closes.</param>
		/// <param name="LinkService">Reads and validates invitations that are pasted or opened as links.</param>
		public WelcomeOnboardingStepViewModel(IQrService QrService, ILinkService LinkService) : base(OnboardingStep.Welcome)
		{
			this.qrService = QrService;
			this.linkService = LinkService;
		}

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
		public new bool IsBusy => this.IsLoading || this.IsProcessingInvite;

		internal override async Task OnActivatedAsync()
		{
			// Returning to this step must accept a new invite, unless one is still being processed.
			if (!this.IsProcessingInvite)
				this.autoAdvanced = false;

			ServiceRef.LogService.LogInformational("Welcome step activated.");
			await base.OnActivatedAsync();
			if (this.IsLoading)
			{
				ServiceRef.LogService.LogDebug("Simulating initial loading delay.");
				await Task.Delay(800);
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
			if (this.autoAdvanced)
			{
				ServiceRef.LogService.LogDebug("Invite code change ignored: already auto-advanced.");
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
			ServiceRef.LogService.LogInformational("Invite code validated.",
				new KeyValuePair<string, object?>("Valid", IsValid),
				new KeyValuePair<string, object?>("Length", Trimmed.Length));
			if (!IsValid)
				return;

			CancellationTokenSource DebounceCts = new CancellationTokenSource();
			this.inviteCodeCts = DebounceCts;
			ServiceRef.LogService.LogDebug($"Debounce scheduled ({inviteCodeDebounceMs} ms) for invitation processing.");

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(inviteCodeDebounceMs, DebounceCts.Token);
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
						bool Processed = await this.ReadAndProcessInvitationAsync(Trimmed, DebounceCts.Token);
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

		/// <summary>
		/// Processes an invitation that was opened as a link, or prepared before onboarding started.
		/// </summary>
		/// <param name="InviteUrl">The invitation URI.</param>
		/// <param name="PreparedInvitation">The invitation already read and validated for <paramref name="InviteUrl"/>, so it is not read again,
		/// or <c>null</c> to read it now.</param>
		/// <returns>A task representing invitation processing and onboarding advancement.</returns>
		public async Task HandleInviteCodeFromIntent(string InviteUrl, Invitation? PreparedInvitation = null)
		{
			ServiceRef.LogService.LogInformational("Handling invitation from intent.",
				new KeyValuePair<string, object?>("Prepared", PreparedInvitation is not null));

			bool IsValid = BasicValidateInviteCode(InviteUrl, out string Trimmed);
			MainThread.BeginInvokeOnMainThread(() => this.InviteCodeIsValid = IsValid);
			if (!IsValid)
			{
				ServiceRef.LogService.LogWarning("Invite code failed basic validation.");
				return;
			}

			this.SetInviteProcessingState(true);
			try
			{
				bool Processed = PreparedInvitation is not null
					? await this.ProcessInvitationAsync(PreparedInvitation)
					: await this.ReadAndProcessInvitationAsync(Trimmed, CancellationToken.None);
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
			this.CancelPendingInviteProcessing();

			// The scanner reads and validates the invitation before it closes, and shows any problem itself.
			Invitation? Invitation = await this.qrService.ScanInvitationAsync(nameof(AppResources.ScanQRCode));
			if (Invitation is null)
			{
				ServiceRef.LogService.LogDebug("Invitation scan cancelled.");
				return;
			}

			ServiceRef.LogService.LogInformational("Valid onboarding QR code scanned.");
			this.SetInviteProcessingState(true);
			try
			{
				if (await this.ProcessInvitationAsync(Invitation))
					MainThread.BeginInvokeOnMainThread(this.AdvanceAfterInvite);
			}
			finally
			{
				this.SetInviteProcessingState(false);
			}
		}

		[RelayCommand]
		private async Task SelectForMe()
		{
			ServiceRef.LogService.LogInformational("SelectForMe command invoked from Welcome step.");

			this.CancelPendingInviteProcessing();

			if (!await this.PrepareAutomaticProviderSelectionAsync())
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
				await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.ValidatePhone);
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
				this.autoAdvanced = false;
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

		/// <summary>
		/// Retrieves, decrypts, and validates an invitation, and then applies it.
		/// </summary>
		/// <param name="InviteUrl">The invitation URI.</param>
		/// <param name="CancellationToken">Cancels reading the invitation, for example when the code is edited.</param>
		/// <returns>Whether onboarding should advance to the next step.</returns>
		private async Task<bool> ReadAndProcessInvitationAsync(string InviteUrl, CancellationToken CancellationToken)
		{
			PreparationResult<Invitation> Result = await this.linkService.ReadInvitationAsync(InviteUrl, CancellationToken);
			if (Result.Status == PreparationStatus.Cancelled)
				return false;

			if (Result.Value is not Invitation Invitation)
			{
				this.MarkInviteAsInvalid(Result.Status == PreparationStatus.Unavailable
					? nameof(AppResources.UnableToAccessInvitation)
					: nameof(AppResources.InvalidInvitationCode));
				return false;
			}

			return await this.ProcessInvitationAsync(Invitation);
		}

		/// <summary>
		/// Applies a validated invitation: selects its provider, or prepares the transfer of its account.
		/// </summary>
		/// <param name="Invitation">The validated invitation.</param>
		/// <returns>Whether onboarding should advance to the next step.</returns>
		private async Task<bool> ProcessInvitationAsync(Invitation Invitation)
		{
			ServiceRef.LogService.LogInformational("Processing invitation.");

			XmlElement? LegalIdDefinition = null;
			string? transferredAccount = null;
			string? transferredPassword = null;
			string? transferredPasswordMethod = null;
			bool domainSelectedFromInvite = false;
			try
			{
				XmlElement? Root = Invitation.Definition.DocumentElement;
				if (Root is null)
					throw new FormatException("Invitation has no content.");

				LinkedList<XmlElement> ToProcess = new LinkedList<XmlElement>();
				ToProcess.AddLast(Root);
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
							if (string.IsNullOrEmpty(ApiDomain))
							{
								ApiDomain = await ServiceRef.UiService.DisplayPrompt("Please enter the IP of your provider", "This is for developers and testers", "Enter", "Cancel") ?? string.Empty;
							}
							ServiceRef.LogService.LogInformational($"Selecting domain (ApiKey) '{ApiDomain}'.");
							await SelectDomain(ApiDomain, ApiKey, Secret);
							domainSelectedFromInvite = true;
							break;
						case "Account":
							string UserName = XML.Attribute(E, "userName");
							string Password = XML.Attribute(E, "password");
							string PasswordMethod = XML.Attribute(E, "passwordMethod");
							string AccDomain = XML.Attribute(E, "domain");
							ServiceRef.LogService.LogInformational($"Selecting domain (Account) '{AccDomain}'. Capturing account transfer.");
							await SelectDomain(AccDomain, string.Empty, string.Empty);
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
					await this.CoordinatorViewModel.ApplyTransferContextAsync(Context);
					shouldAdvance = true;

					bool Connected = await this.CoordinatorViewModel.TryFinalizeTransferAsync(false);
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
