using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Contacts;
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
using NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionContract;
using NeuroAccessMaui.UI.Pages.Kyc;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;

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
						return this.RouteTokenAsync(CancellationToken);
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
					reference = await Database.FindFirstIgnoreRest<KycReference>(new FilterFieldEqualTo(nameof(KycReference.CreatedIdentityId), Intent.EntityId));
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}

				bool isApproved = state == IdentityState.Approved || reference?.CreatedIdentityState == IdentityState.Approved;

				if (reference is not null && !isApproved)
				{
					KycProcessNavigationArgs args = new(reference);
					await ServiceRef.NavigationService.GoToAsync(nameof(KycProcessPage), args);
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
			if (string.IsNullOrEmpty(Intent.EntityId))
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage));
				return NotificationRouteResult.Success;
			}

			try
			{
				Contract contract = await ServiceRef.XmppService.GetContract(Intent.EntityId);
				ViewContractNavigationArgs args = new(contract, false);
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewContractPage), args);
				return NotificationRouteResult.Success;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage));
				return NotificationRouteResult.Success;
			}
		}

		private async Task<NotificationRouteResult> RouteTokenAsync(CancellationToken CancellationToken)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(MyTokensPage));
			return NotificationRouteResult.Success;
		}

		private async Task<NotificationRouteResult> RouteBalanceAsync(CancellationToken CancellationToken)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(WalletPage));
			return NotificationRouteResult.Success;
		}

		private async Task<NotificationRouteResult> RoutePetitionAsync(NotificationIntent Intent, CancellationToken CancellationToken)
		{
			try
			{
				if (Intent.Extras.TryGetValue("requestedIdentityId", out string? RequestedIdentityId))
				{
					LegalIdentity? RequestorIdentity = null;
					if (!string.IsNullOrEmpty(Intent.EntityId))
					{
						ContactInfo? Info = await ContactInfo.FindByBareJid(Intent.EntityId);
						RequestorIdentity = Info?.LegalIdentity;
					}

					(bool Succeeded, LegalIdentity? RequestedIdentity) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(RequestedIdentityId));
					if (!Succeeded || RequestedIdentity is null)
					{
						string Title = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
						string Message = "Petition has expired or is no longer available.";
						await ServiceRef.UiService.DisplayAlert(Title, Message);
						return NotificationRouteResult.Failed;
					}
					else
					{
						RequestorIdentity = RequestedIdentity;
					}

					string? PetitionId = Intent.Extras.TryGetValue("petitionId", out string? pid) ? pid : null;
					PetitionIdentityNavigationArgs Args = new(RequestorIdentity, Intent.EntityId, RequestedIdentityId, PetitionId, Intent.Body);
					await ServiceRef.NavigationService.GoToAsync(nameof(PetitionIdentityPage), Args);
					return NotificationRouteResult.Success;
				}

				if (Intent.Extras.TryGetValue("contractId", out string? contractId))
				{
					Contract? Contract = null;
					try
					{
						Contract = await ServiceRef.XmppService.GetContract(contractId);
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}

					string? PetitionId = Intent.Extras.TryGetValue("petitionId", out string? Pid) ? Pid : null;
					PetitionContractNavigationArgs Args = new(null, Intent.EntityId, Contract, PetitionId, Intent.Body);
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

		private async Task<NotificationRouteResult> RouteSettingsAsync(CancellationToken CancellationToken)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(SettingsPage));
			return NotificationRouteResult.Success;
		}
	}
}
