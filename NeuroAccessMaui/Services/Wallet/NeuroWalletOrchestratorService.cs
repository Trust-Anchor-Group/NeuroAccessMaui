using EDaler;
using EDaler.Events;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Wallet;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.IssueEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.Payment;
using NeuroAccessMaui.UI.Pages.Wallet.PaymentAcceptance;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Wallet
{
	[Singleton]
	internal class NeuroWalletOrchestratorService : LoadableService, INeuroWalletOrchestratorService
	{
		public NeuroWalletOrchestratorService()
		{
		}

		public override Task Load(bool isResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(isResuming, CancellationToken))
			{
				ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
				ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
				ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
				ServiceRef.XmppService.NeuroFeatureStateUpdated += Wallet_StateUpdated;
				ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;

				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				ServiceRef.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
				ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
				ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
				ServiceRef.XmppService.NeuroFeatureStateUpdated -= Wallet_StateUpdated;
				ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;

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

		private async Task Wallet_TokenAdded(object? Sender, TokenEventArgs e)
		{
			string Title = ServiceRef.Localizer[nameof(AppResources.NotificationTokenAddedTitle)];
			string Body = ServiceRef.Localizer[nameof(AppResources.NotificationTokenAddedBody)];

			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Tokens,
				Title = Title,
				Body = Body,
				Action = NotificationAction.OpenToken,
				EntityId = e.Token.TokenId
			};

			await ServiceRef.Provider.GetRequiredService<INotificationServiceV2>().AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
		}

		private async Task Wallet_TokenRemoved(object? Sender, TokenEventArgs e)
		{
			string Title = ServiceRef.Localizer[nameof(AppResources.NotificationTokenRemovedTitle)];
			string Body = ServiceRef.Localizer[nameof(AppResources.NotificationTokenRemovedBody)];

			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Tokens,
				Title = Title,
				Body = Body,
				Action = NotificationAction.OpenToken,
				EntityId = e.Token.TokenId
			};

			await ServiceRef.Provider.GetRequiredService<INotificationServiceV2>().AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
		}

		private static async Task Wallet_StateUpdated(object? Sender, NewStateEventArgs e)
		{
			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Tokens,
				Title = ServiceRef.Localizer[nameof(AppResources.State)],
				Action = NotificationAction.OpenToken,
				EntityId = e.TokenId
			};

			await ServiceRef.Provider.GetRequiredService<INotificationServiceV2>().AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
		}

		private async Task Wallet_VariablesUpdated(object? Sender, VariablesUpdatedEventArgs e)
		{
			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Tokens,
				Title = ServiceRef.Localizer[nameof(AppResources.State)],
				Action = NotificationAction.OpenToken,
				EntityId = e.TokenId
			};

			await ServiceRef.Provider.GetRequiredService<INotificationServiceV2>().AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
		}

		#endregion

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

			try
			{
				Token Token = await ServiceRef.XmppService.GetNeuroFeature(TokenId);

				if (!ServiceRef.NotificationService.TryGetNotificationEvents(NotificationEventType.Wallet, TokenId, out NotificationEvent[]? Events))
					Events = [];

				TokenDetailsNavigationArgs Args = new(new TokenItem(Token, Events));

				await ServiceRef.NavigationService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

	}
}
