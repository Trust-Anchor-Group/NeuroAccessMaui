using EDaler;
using EDaler.Events;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Wallet;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.IssueEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.Payment;
using NeuroAccessMaui.UI.Pages.Wallet.PaymentAcceptance;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Wallet
{
	[Singleton]
	internal class NeuroWalletOrchestratorService : LoadableService, INeuroWalletOrchestratorService
	{
		/// <summary>
		/// How long after the user starts creating tokens that new tokens they created are reported.
		/// </summary>
		private static readonly TimeSpan creationWatchDuration = TimeSpan.FromMinutes(30);

		/// <summary>
		/// How long to wait for more created tokens before reporting them, since one contract can create several.
		/// </summary>
		private const int CreationReportDelayMs = 3000;

		private readonly object creationSynch = new();
		private readonly List<Token> createdTokens = [];
		private DateTime creationWatchStarted = DateTime.MinValue;
		private int creationBatch;
		private bool legacyCleanupStarted;

		public NeuroWalletOrchestratorService()
		{
		}

		/// <inheritdoc/>
		public bool AreTokensEnabled =>
			ServiceRef.TagProfile.HasBetaFeatures && !string.IsNullOrEmpty(ServiceRef.TagProfile.NeuroFeaturesJid);

		/// <inheritdoc/>
		public override Task Load(bool isResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(isResuming, CancellationToken))
			{
				ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
				ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;

				this.EndLoad(true);

				if (!this.legacyCleanupStarted)
				{
					this.legacyCleanupStarted = true;
					// The cleanup runs once in the background, and isn't tied to this load.
					_ = Task.Run(DeleteLegacyTokenEventsAsync, CancellationToken.None);
				}
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public override Task Unload()
		{
			if (this.IsLoading || this.BeginUnload())
			{
				ServiceRef.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
				ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;

				this.IsLoading = false;
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private async Task Wallet_BalanceUpdated(object? Sender, BalanceEventArgs e)
		{
			if (e.Balance.Amount > 0 && !ServiceRef.TagProfile.HasWallet)
				ServiceRef.TagProfile.HasWallet = true;

			string Title = ServiceRef.Localizer[nameof(AppResources.NotificationBalanceUpdatedTitle)];
			string Body = ServiceRef.Localizer[nameof(AppResources.NotificationBalanceUpdatedBody)];

			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.EDaler,
				Title = Title,
				Body = Body,
				Action = NotificationAction.OpenBalance,
				EntityId = e.Balance.Currency
			};

			await ServiceRef.Provider.GetRequiredService<INotificationServiceV2>().AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
		}

		/// <summary>
		/// Collects the tokens the user created, while the user is creating tokens.
		/// </summary>
		private Task Wallet_TokenAdded(object? Sender, TokenEventArgs e)
		{
			Token? Token = e.Token;
			if (Token is null)
				return Task.CompletedTask;

			int Batch;

			lock (this.creationSynch)
			{
				DateTime Started = this.creationWatchStarted;

				if (Started == DateTime.MinValue || DateTime.UtcNow - Started > creationWatchDuration)
					return Task.CompletedTask;

				// Tokens created earlier, that come back to the user, aren't reported.
				if (!string.Equals(Token.CreatorJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase) ||
					Token.Created.ToUniversalTime() < Started.AddMinutes(-5))
				{
					return Task.CompletedTask;
				}

				this.createdTokens.Add(Token);
				Batch = ++this.creationBatch;
			}

			_ = this.ReportCreatedTokensAsync(Batch);

			return Task.CompletedTask;
		}

		#endregion

		/// <inheritdoc/>
		public async Task CreateTokenAsync()
		{
			try
			{
				TaskCompletionSource<Contract?> TemplateSelection = new();
				MyContractsNavigationArgs Args = new(ContractsListMode.TokenCreationTemplates, TemplateSelection);

				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);

				Contract? Template = await TemplateSelection.Task;
				if (Template is null)
					return;

				NewContractNavigationArgs NewContractArgs = await ServiceRef.TokenTemplateService.PrepareAsync(TokenAction.Create,
					Template, new TokenContractContext());

				lock (this.creationSynch)
				{
					this.creationWatchStarted = DateTime.UtcNow;
					this.createdTokens.Clear();
				}

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await TokenErrors.ShowAsync(Ex);
			}
		}

		/// <summary>
		/// Reports the tokens the user created, once no more have arrived for a moment. A single token opens when the toast
		/// is tapped, and several open My Tokens.
		/// </summary>
		/// <param name="Batch">The arrival that started this report. Later arrivals report instead.</param>
		private async Task ReportCreatedTokensAsync(int Batch)
		{
			try
			{
				await Task.Delay(CreationReportDelayMs);

				Token[] Created;

				lock (this.creationSynch)
				{
					if (Batch != this.creationBatch)
						return;

					Created = [.. this.createdTokens];
					this.createdTokens.Clear();
					this.creationWatchStarted = DateTime.MinValue;
				}

				if (Created.Length == 1)
				{
					string TokenId = Created[0].TokenId;

					await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.TokenCreated)],
						() => this.OpenTokenAsync(TokenId, null));
				}
				else if (Created.Length > 1)
				{
					await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.TokensCreatedCount), Created.Length],
						() => GoToMyTokensAsync(null));
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Deletes the token notification events that earlier versions of the app stored, once. Token notifications are
		/// stored as notification records now, and the old events are no longer shown anywhere.
		/// </summary>
		private static async Task DeleteLegacyTokenEventsAsync()
		{
			try
			{
				if (await RuntimeSettings.GetAsync(Constants.Settings.LegacyTokenEventsDeleted, false))
					return;

				await ServiceRef.NotificationService.Load(false, CancellationToken.None);

				List<NotificationEvent> ToDelete = [];

#pragma warning disable CS0618 // The old event types are kept only so that stored events can be loaded and deleted.
				SortedDictionary<CaseInsensitiveString, TokenNotificationEvent[]> EventsByToken =
					ServiceRef.NotificationService.GetEventsByCategory<TokenNotificationEvent>(NotificationEventType.Wallet);

				foreach (TokenNotificationEvent[] Events in EventsByToken.Values)
				{
					foreach (TokenNotificationEvent Event in Events)
					{
						if (Event is TokenAddedNotificationEvent ||
							Event is TokenRemovedNotificationEvent ||
							Event is StateMachineNewStateNotificationEvent ||
							Event is StateMachineVariablesUpdatedNotificationEvent)
						{
							ToDelete.Add(Event);
						}
					}
				}
#pragma warning restore CS0618

				if (ToDelete.Count > 0)
					await ServiceRef.NotificationService.DeleteEvents([.. ToDelete]);

				await RuntimeSettings.SetAsync(Constants.Settings.LegacyTokenEventsDeleted, true);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Opens the wallet
		/// </summary>
		public async Task OpenEDalerWallet()
		{
			try
			{
				Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();
				(decimal PendingAmount, string PendingCurrency, PendingPayment[] PendingPayments) = await ServiceRef.XmppService.GetPendingEDalerPayments();
				(AccountEvent[] Events, bool More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);

				WalletNavigationArgs e = new(Balance, PendingAmount, PendingCurrency, PendingPayments, Events, More);

				await ServiceRef.NavigationService.GoToAsync(nameof(MyEDalerWalletPage), e);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// eDaler URI scanned.
		/// </summary>
		/// <param name="Uri">eDaler URI.</param>
		public async Task OpenEDalerUri(string Uri)
		{
			if (!ServiceRef.XmppService.TryParseEDalerUri(Uri, out EDalerUri Parsed, out string Reason))
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.InvalidEDalerUri), Reason]);
				return;
			}

			if (Parsed.Expires.AddDays(1) < DateTime.UtcNow)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.ExpiredEDalerUri)]);
				return;
			}

			if (Parsed is EDalerIssuerUri)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(IssueEDalerPage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else if (Parsed is EDalerDestroyerUri)
			{
				// TODO
			}
			else if (Parsed is EDalerPaymentUri)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(PaymentAcceptancePage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else if (Parsed is EDalerIncompletePaymentUri)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(PaymentPage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.UnrecognizedEDalerURI)]);
				return;
			}
		}

		/// <summary>
		/// Neuro-Feature URI scanned.
		/// </summary>
		/// <param name="Uri">Neuro-Feature URI.</param>
		public async Task OpenNeuroFeatureUri(string Uri)
		{
			int i = Uri.IndexOf(':');
			if (i < 0)
				return;

			string TokenId = Uri[(i + 1)..];
			if (string.IsNullOrEmpty(TokenId))
				return;

			await this.OpenTokenAsync(TokenId, null);
		}

		/// <inheritdoc/>
		public async Task<bool> OpenTokenAsync(string TokenId, Token? SharedCopy)
		{
			try
			{
				Token Live;

				try
				{
					Live = await ServiceRef.XmppService.GetNeuroFeature(TokenId);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);

					if (SharedCopy is null)
					{
						await TokenErrors.ShowAsync(Ex);
						return false;
					}

					await GoToTokenDetailsAsync(SharedCopy, TokenOrigin.UnverifiedCopy, false);
					return true;
				}

				bool Changed = SharedCopy is not null && HasChangedSinceShared(SharedCopy, Live);
				await GoToTokenDetailsAsync(Live, TokenOrigin.Live, Changed);
				return true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
				return false;
			}
		}

		/// <inheritdoc/>
		public async Task OpenTokenFromNotificationAsync(string? TokenId)
		{
			try
			{
				if (string.IsNullOrEmpty(TokenId))
				{
					await GoToMyTokensAsync(null);
					return;
				}

				Token Token;

				try
				{
					Token = await ServiceRef.XmppService.GetNeuroFeature(TokenId);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);

					string Message = TokenErrors.IsUnreachable(Ex)
						? ServiceRef.Localizer[nameof(AppResources.TokenNeuronUnreachable)]
						: ServiceRef.Localizer[nameof(AppResources.TokenNoLongerInWallet)];

					await GoToMyTokensAsync(Message);
					return;
				}

				if (string.Equals(Token.OwnerJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase))
					await GoToTokenDetailsAsync(Token, TokenOrigin.Live, false);
				else
					await GoToMyTokensAsync(ServiceRef.Localizer[nameof(AppResources.TokenNoLongerInWallet)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Opens token details.
		/// </summary>
		/// <param name="Token">The token to show.</param>
		/// <param name="Origin">Where the token comes from.</param>
		/// <param name="ChangedSinceShared">If the token has changed since a copy of it was shared.</param>
		private static Task GoToTokenDetailsAsync(Token Token, TokenOrigin Origin, bool ChangedSinceShared)
		{
			if (Origin != TokenOrigin.UnverifiedCopy)
				ServiceRef.TokenNameCache.Remember(Token);

			TokenDetailsNavigationArgs Args = new(new TokenItem(Token), Origin, ChangedSinceShared);
			return ServiceRef.NavigationService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
		}

		/// <summary>
		/// Opens My Tokens, and shows a message as a toast, if one is given.
		/// </summary>
		/// <param name="Message">The message to show, if any.</param>
		private static async Task GoToMyTokensAsync(string? Message)
		{
			await ServiceRef.NavigationService.GoToAsync(nameof(MyTokensPage));

			if (!string.IsNullOrEmpty(Message))
				await ServiceRef.ToastService.ShowTextAsync(Message);
		}

		/// <summary>
		/// Checks if the live token differs from a copy that was shared earlier.
		/// </summary>
		/// <param name="SharedCopy">The shared copy.</param>
		/// <param name="Live">The token, as fetched from its Neuron.</param>
		/// <returns>If the token has changed since it was shared.</returns>
		private static bool HasChangedSinceShared(Token SharedCopy, Token Live)
		{
			if (SharedCopy.Updated != Live.Updated)
				return true;

			byte[]? SharedSignature = SharedCopy.Signature;
			byte[]? LiveSignature = Live.Signature;

			if (SharedSignature is null || LiveSignature is null)
				return (SharedSignature is null) != (LiveSignature is null);

			return !SharedSignature.AsSpan().SequenceEqual(LiveSignature);
		}
	}
}
