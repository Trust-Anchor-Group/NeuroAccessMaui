using System;
using System.Threading;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using NeuroAccessMaui.Pages.Wallet;
using NeuroAccessMaui.Pages.Wallet.IssueEDaler;
using NeuroAccessMaui.Pages.Wallet.MyWallet;
using NeuroAccessMaui.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.Pages.Wallet.Payment;
using NeuroAccessMaui.Pages.Wallet.PaymentAcceptance;
using NeuroAccessMaui.Pages.Wallet.TokenDetails;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Wallet;
using NeuroFeatures;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace NeuroAccessMaui.Services.Wallet
{
	[Singleton]
	internal class NeuroWalletOrchestratorService : LoadableService, INeuroWalletOrchestratorService
	{
		public NeuroWalletOrchestratorService()
		{
		}

		public override Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (this.BeginLoad(isResuming, cancellationToken))
			{
				ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
				ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
				ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
				ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
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
				ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;
				ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;

				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private async Task Wallet_BalanceUpdated(object Sender, BalanceEventArgs e)
		{
			await ServiceRef.NotificationService.NewEvent(new BalanceNotificationEvent(e));
		}

		private async Task Wallet_TokenAdded(object Sender, TokenEventArgs e)
		{
			await ServiceRef.NotificationService.NewEvent(new TokenAddedNotificationEvent(e));
		}

		private async Task Wallet_TokenRemoved(object Sender, TokenEventArgs e)
		{
			if (ServiceRef.NotificationService.TryGetNotificationEvents(NotificationEventType.Wallet, e.Token.TokenId, out NotificationEvent[]? Events))
				await ServiceRef.NotificationService.DeleteEvents(Events);

			await ServiceRef.NotificationService.NewEvent(new TokenRemovedNotificationEvent(e));
		}

		private async Task Wallet_StateUpdated(object Sender, NewStateEventArgs e)
		{
			await ServiceRef.NotificationService.NewEvent(new StateMachineNewStateNotificationEvent(e));
		}

		private async Task Wallet_VariablesUpdated(object Sender, VariablesUpdatedEventArgs e)
		{
			await ServiceRef.NotificationService.NewEvent(new StateMachineVariablesUpdatedNotificationEvent(e));
		}

		#endregion

		/// <summary>
		/// Opens the wallet
		/// </summary>
		public async Task OpenWallet()
		{
			try
			{
				Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();
				(decimal PendingAmount, string PendingCurrency, PendingPayment[] PendingPayments) = await ServiceRef.XmppService.GetPendingEDalerPayments();
				(AccountEvent[] Events, bool More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);

				WalletNavigationArgs e = new(Balance, PendingAmount, PendingCurrency, PendingPayments, Events, More);

				await ServiceRef.NavigationService.GoToAsync(nameof(MyWalletPage), e);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiSerializer.DisplayAlert(ex);
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
				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.InvalidEDalerUri), Reason]);
				return;
			}

			if (Parsed.Expires.AddDays(1) < DateTime.UtcNow)
			{
				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
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
				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
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

				TokenDetailsNavigationArgs Args = new(new TokenItem(Token, this, Events));

				await ServiceRef.NavigationService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

	}
}
