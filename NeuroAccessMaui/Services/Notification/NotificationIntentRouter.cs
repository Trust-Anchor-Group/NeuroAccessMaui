using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionContract;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature;
using NeuroAccessMaui.UI.Pages.Kyc;
using NeuroAccessMaui.UI.Pages.Main;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Security;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Default router for notification intents. Wiring to navigation can be extended here.
	/// </summary>
	public sealed class NotificationIntentRouter : INotificationIntentRouter
	{
		private readonly INotificationFilterRegistry filterRegistry;

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationIntentRouter"/> class.
		/// </summary>
		/// <param name="FilterRegistry">Filter registry deciding when to ignore a notification.</param>
		public NotificationIntentRouter(INotificationFilterRegistry FilterRegistry)
		{
			this.filterRegistry = FilterRegistry;
		}

		/// <inheritdoc/>
		public Task<NotificationRouteResult> RouteAsync(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken)
		{
			try
			{
				if (ServiceRef.NavigationService.CurrentPage is null)
					return Task.FromResult(NotificationRouteResult.Deferred);

				NotificationFilterDecision decision = this.filterRegistry.ShouldIgnore(Intent, FromUserInteraction, CancellationToken);
				if (decision.IgnoreRoute)
					return Task.FromResult(NotificationRouteResult.Ignored);

				switch (Intent.Action)
				{
					case NotificationAction.OpenChat:
						return this.RouteChatAsync(Intent, CancellationToken);
					case NotificationAction.OpenProfile:
						return this.RouteProfileAsync(Intent, CancellationToken);
					case NotificationAction.OpenIdentity:
						return this.RouteIdentityAsync(Intent, CancellationToken);
					case NotificationAction.OpenContract:
						return this.RouteContractAsync(Intent, CancellationToken);
					case NotificationAction.OpenToken:
						return this.RouteTokenAsync(Intent, CancellationToken);
					case NotificationAction.OpenBalance:
						return this.RouteBalanceAsync(CancellationToken);
					case NotificationAction.OpenPetition:
						return this.RoutePetitionAsync(Intent, CancellationToken);
					case NotificationAction.OpenPresenceRequest:
						return this.RoutePresenceAsync(Intent, CancellationToken);
					case NotificationAction.OpenSettings:
						return this.RouteSettingsAsync(CancellationToken);
					default:
						return Task.FromResult(NotificationRouteResult.NoHandler);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return Task.FromResult(NotificationRouteResult.Failed);
			}
		}

		private async Task<NotificationRouteResult> RouteChatAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(Intent.EntityId))
				return NotificationRouteResult.NoHandler;

			ContactInfo? Info = await ContactInfo.FindByBareJid(Intent.EntityId);
			string? FriendlyName = Info?.FriendlyName ?? Intent.EntityId;
			string? LegalId = Info?.LegalId;

			ChatNavigationArgs Args = new(LegalId, Intent.EntityId, FriendlyName);
			await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args);
			return NotificationRouteResult.Success;
		}

		private Task<NotificationRouteResult> RouteProfileAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			// No specific profile route mapped yet; fall back to contacts.
			return this.RoutePresenceAsync(Intent, CancellationToken);
		}

		private async Task<NotificationRouteResult> RoutePresenceAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage));
			return NotificationRouteResult.Deferred;
		}

		private async Task<NotificationRouteResult> RouteIdentityAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(Intent.EntityId))
				return NotificationRouteResult.NoHandler;

			try
			{
				IdentityState? state = null;
				if (Intent.Extras.TryGetValue("state", out string? stateString) && Enum.TryParse(stateString, out IdentityState parsed))
					state = parsed;

				KycReference? reference = null;
				try
				{
					reference = await ServiceRef.KycService.FindReferenceByIdentityIdAsync(Intent.EntityId);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}

				bool isApproved = state == IdentityState.Approved || reference?.GetEffectiveApplicationIdentityState() == IdentityState.Approved;

				if (reference is not null && !isApproved)
				{
					KycProcessNavigationArgs args = new(reference);
					await ServiceRef.NavigationService.GoToAsync(nameof(KycApplicationStatusPage), args);
					return NotificationRouteResult.Success;
				}

				LegalIdentity identity = await ServiceRef.XmppService.GetLegalIdentity(Intent.EntityId);
				ViewIdentityNavigationArgs viewArgs = new(identity);
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), viewArgs);
				return NotificationRouteResult.Success;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return NotificationRouteResult.Failed;
			}
		}

		private async Task<NotificationRouteResult> RouteContractAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			string? ContractId = Intent.EntityId;
			if (string.IsNullOrEmpty(ContractId))
				ContractId = GetExtra(Intent.Extras, "contractId");

			if (string.IsNullOrEmpty(ContractId))
			{
				await GoToMyContractsAsync();
				return NotificationRouteResult.Success;
			}

			ContractOpenOptions? Options = null;
			if (string.Equals(GetExtra(Intent.Extras, "kind"), "invitation", StringComparison.OrdinalIgnoreCase))
			{
				Options = new ContractOpenOptions()
				{
					InvitedRole = GetExtra(Intent.Extras, "role"),
					InvitedBy = GetExtra(Intent.Extras, "fromJid"),
					InvitationMessage = GetExtra(Intent.Extras, "message")
				};
			}

			// When the contract can't be opened, the user has been told why, and lands in the list of contracts.
			if (!await ServiceRef.ContractOrchestratorService.OpenContractAsync(ContractId, Options))
				await GoToMyContractsAsync();

			return NotificationRouteResult.Success;
		}

		private static Task GoToMyContractsAsync()
		{
			return ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), new MyContractsNavigationArgs(ContractsListMode.Contracts));
		}

		private async Task<NotificationRouteResult> RouteTokenAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			string? TokenId = Intent.EntityId;
			if (string.IsNullOrEmpty(TokenId))
				TokenId = GetExtra(Intent.Extras, "tokenId");

			await ServiceRef.NeuroWalletOrchestratorService.OpenTokenFromNotificationAsync(TokenId);
			return NotificationRouteResult.Success;
		}

		private async Task<NotificationRouteResult> RouteBalanceAsync(CancellationToken CancellationToken)
		{
			MainNavigationArgs args = new()
			{
				TargetTabKey = "wallet"
			};

			await ServiceRef.NavigationService.GoToAsync(nameof(MainPage), args);
			return NotificationRouteResult.Success;
		}

		private async Task<NotificationRouteResult> RoutePetitionAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			try
			{
				// A push carries less than the XMPP message. Details from a stored XMPP notification with the same petition ID come first.
				(Dictionary<string, string> Extras, string? IdentityXml) = await GetPetitionDetailsAsync(Intent, CancellationToken);
				string? PetitionId = GetExtra(Extras, "petitionId") ?? Intent.CorrelationId;
				string? RequestorFullJid = !string.IsNullOrEmpty(Intent.EntityId) ? Intent.EntityId : GetExtra(Extras, "from");

				string? RequestedIdentityId = GetExtra(Extras, "requestedIdentityId");
				if (!string.IsNullOrEmpty(RequestedIdentityId))
				{
					if (string.IsNullOrEmpty(PetitionId) || string.IsNullOrEmpty(RequestorFullJid))
						return await ShowPetitionNoLongerAvailableAsync();

					LegalIdentity? RequestorIdentity = await GetRequestorIdentityAsync(IdentityXml, GetExtra(Extras, "requestorIdentityId"), RequestorFullJid);
					PetitionIdentityNavigationArgs Args = new(RequestorIdentity, RequestorFullJid, RequestedIdentityId, PetitionId, GetExtra(Extras, "purpose"));
					await ServiceRef.NavigationService.GoToAsync(nameof(PetitionIdentityPage), Args);
					return NotificationRouteResult.Success;
				}

				if (Extras.TryGetValue("signatoryId", out string? SignatoryIdentityId) &&
					Extras.TryGetValue("contentToSign", out string? ContentToSignBase64) &&
					!string.IsNullOrEmpty(SignatoryIdentityId) &&
					!string.IsNullOrEmpty(ContentToSignBase64))
				{
					LegalIdentity? RequestorIdentity = null;
					if (Extras.TryGetValue("requestorIdentityId", out string? RequestorIdentityId) &&
						!string.IsNullOrEmpty(RequestorIdentityId))
					{
						(bool Succeeded, LegalIdentity? LegalId) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(RequestorIdentityId));
						if (Succeeded && LegalId is not null)
							RequestorIdentity = LegalId;
					}

					if (RequestorIdentity is null && !string.IsNullOrEmpty(RequestorFullJid))
					{
						ContactInfo? Info = await ContactInfo.FindByBareJid(RequestorFullJid);
						RequestorIdentity = Info?.LegalIdentity;
					}

					if (RequestorIdentity is null || string.IsNullOrEmpty(RequestorFullJid))
					{
						string Title = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
						string Message = ServiceRef.Localizer[nameof(AppResources.PetitionNoLongerAvailable)];
						await ServiceRef.UiService.DisplayAlert(Title, Message);
						return NotificationRouteResult.Failed;
					}

					byte[] ContentToSign;
					try
					{
						ContentToSign = Convert.FromBase64String(ContentToSignBase64);
					}
					catch (FormatException ex)
					{
						ServiceRef.LogService.LogException(ex);
						string Title = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
						string Message = ServiceRef.Localizer[nameof(AppResources.PetitionNoLongerAvailable)];
						await ServiceRef.UiService.DisplayAlert(Title, Message);
						return NotificationRouteResult.Failed;
					}

					string Purpose = Extras.TryGetValue("purpose", out string? PurposeValue)
						? PurposeValue ?? string.Empty
						: string.Empty;

					PetitionSignatureNavigationArgs Args = new(RequestorIdentity, RequestorFullJid, SignatoryIdentityId,
						ContentToSign, PetitionId ?? string.Empty, Purpose);
					await ServiceRef.NavigationService.GoToAsync(nameof(PetitionSignaturePage), Args);
					return NotificationRouteResult.Success;
				}

				string? ContractId = GetExtra(Extras, "contractId");
				if (!string.IsNullOrEmpty(ContractId))
				{
					if (string.IsNullOrEmpty(PetitionId) || string.IsNullOrEmpty(RequestorFullJid))
						return await ShowPetitionNoLongerAvailableAsync();

					LegalIdentity? RequestorIdentity = await GetRequestorIdentityAsync(IdentityXml, GetExtra(Extras, "requestorIdentityId"), RequestorFullJid);
					Contract? RequestedContract = await GetRequestedContractAsync(ContractId);

					ContractPetitionRequest Request = new(ContractId, PetitionId, RequestorFullJid, RequestorIdentity, GetExtra(Extras, "purpose"));
					PetitionContractNavigationArgs Args = new(Request, RequestedContract);
					await ServiceRef.NavigationService.GoToAsync(nameof(PetitionContractPage), Args);
					return NotificationRouteResult.Success;
				}

				await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage));
				return NotificationRouteResult.Success;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return NotificationRouteResult.Failed;
			}
		}

		/// <summary>
		/// Gets the extras of a petition notification, completed with those of stored notifications for the same petition,
		/// and the requestor's identity XML that the XMPP notification keeps as its raw payload.
		/// </summary>
		private static async Task<(Dictionary<string, string> Extras, string? IdentityXml)> GetPetitionDetailsAsync(NotificationIntent Intent,
			CancellationToken CancellationToken)
		{
			Dictionary<string, string> Extras = new(Intent.Extras, StringComparer.OrdinalIgnoreCase);
			string? PetitionId = GetExtra(Extras, "petitionId") ?? Intent.CorrelationId;
			string? IdentityXml = null;

			if (string.IsNullOrEmpty(PetitionId))
				return (Extras, IdentityXml);

			try
			{
				INotificationServiceV2 NotificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				IReadOnlyList<NotificationRecord> Records = await NotificationService.GetAsync(new NotificationQuery()
				{
					Channels = [Constants.PushChannels.Petitions]
				}, CancellationToken);

				foreach (NotificationRecord Record in Records)
				{
					if (!string.Equals(Record.CorrelationId, PetitionId, StringComparison.Ordinal))
						continue;

					Dictionary<string, string>? Stored = null;
					try
					{
						Stored = JsonSerializer.Deserialize<Dictionary<string, string>>(Record.ExtrasJson);
					}
					catch (JsonException)
					{
						// Ignore extras that can't be read.
					}

					if (Stored is not null)
					{
						foreach (KeyValuePair<string, string> Item in Stored)
						{
							if (string.IsNullOrEmpty(GetExtra(Extras, Item.Key)))
								Extras[Item.Key] = Item.Value;
						}
					}

					if (IdentityXml is null && Record.RawPayload?.TrimStart().StartsWith('<') == true)
						IdentityXml = Record.RawPayload;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return (Extras, IdentityXml);
		}

		/// <summary>
		/// Gets the identity of the person who sent a petition: from the stored notification, from the server, or from the
		/// contact list. Returns null when none is available. The petition can still be answered.
		/// </summary>
		private static async Task<LegalIdentity?> GetRequestorIdentityAsync(string? IdentityXml, string? RequestorIdentityId, string RequestorFullJid)
		{
			if (!string.IsNullOrEmpty(IdentityXml))
			{
				try
				{
					return LegalIdentity.Parse(IdentityXml);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			if (!string.IsNullOrEmpty(RequestorIdentityId))
			{
				(bool Succeeded, LegalIdentity? Identity) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(RequestorIdentityId));
				if (Succeeded && Identity is not null)
					return Identity;
			}

			ContactInfo? Info = await ContactInfo.FindByBareJid(XmppClient.GetBareJID(RequestorFullJid));
			return Info?.LegalIdentity;
		}

		/// <summary>
		/// Gets a contract someone asks to see, from the server or the saved copy. Returns null when neither can be read.
		/// </summary>
		private static async Task<Contract?> GetRequestedContractAsync(string ContractId)
		{
			try
			{
				return await ServiceRef.XmppService.GetContract(ContractId);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			try
			{
				ContractReference? Ref = await ServiceRef.ContractStore.GetAsync(ContractId);
				if (Ref?.ContractLoaded == true)
					return await Ref.GetContract();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return null;
		}

		private static async Task<NotificationRouteResult> ShowPetitionNoLongerAvailableAsync()
		{
			string Title = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
			string Message = ServiceRef.Localizer[nameof(AppResources.PetitionNoLongerAvailable)];
			await ServiceRef.UiService.DisplayAlert(Title, Message);
			return NotificationRouteResult.Failed;
		}

		private static string? GetExtra(IReadOnlyDictionary<string, string> Extras, string Key)
		{
			return Extras.TryGetValue(Key, out string? Value) && !string.IsNullOrEmpty(Value) ? Value : null;
		}

		private async Task<NotificationRouteResult> RouteSettingsAsync(CancellationToken CancellationToken)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(SettingsPage));
			return NotificationRouteResult.Success;
		}
	}
}
