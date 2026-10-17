using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using EDaler.Events;
using EDaler.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Wallet;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.RequestPayment;
using NeuroAccessMaui.UI.Pages.Wallet.SellEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.SendPayment;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;
using NeuroAccessMaui.UI.Popups.Transaction;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script.Constants;
using Waher.Script.Functions.ComplexNumbers;
using NeuroAccessMaui.Services.Authentication;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// ViewModel for the Wallet page, handling balance display, buy flow, and related navigation.
	/// </summary>
	public partial class WalletViewModel : XmppViewModel
	{
		/// <summary>
		/// How many of the most recent tokens the Tokens section shows.
		/// </summary>
		private const int RecentTokenCount = 3;

		/// <summary>
		/// How long the Tokens section waits after a token is added or removed before it refreshes, so a burst of changes
		/// is loaded once.
		/// </summary>
		private static readonly TimeSpan tokensRefreshDelay = TimeSpan.FromSeconds(1);

		private readonly IAuthenticationService authenticationService = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();
		private CancellationTokenSource? tokensRefreshCancel;
		private DateTime lastTokenEvent;

		#region Observable Properties

		/// <summary>
		/// Last fetched balance. Changing this notifies <see cref="BalanceDecimal"/>.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(BalanceDecimal))]
		[NotifyPropertyChangedFor(nameof(BalanceString))]
		[NotifyPropertyChangedFor(nameof(ReservedDecimal))]
		[NotifyPropertyChangedFor(nameof(ReservedString))]
		[NotifyPropertyChangedFor(nameof(HasReserved))]
		[NotifyPropertyChangedFor(nameof(Currency))]
		Balance? fetchedBalance;

		/// <summary>
		/// Indicates if the balance is currently refreshing.
		/// </summary>
		[ObservableProperty]
		private bool isRefreshingBalance;

		/// <summary>
		/// The last known update time of the balance.
		/// </summary>
		[ObservableProperty]
		private DateTime? balanceUpdated = ServiceRef.TagProfile.LastEDalerBalanceUpdate;

		/// <summary>
		/// Gets or sets a value indicating whether the bottom navigation bar should be visible.
		/// </summary>
		[ObservableProperty]
		private bool showBottomNavigation = true;

		#endregion

		#region Computed Properties

		/// <summary>
		/// Currency of current balance.
		/// </summary>
		public string Currency => this.FetchedBalance?.Currency ?? "NC";

		/// <summary>
		/// Exposes the current balance as a decimal.
		/// </summary>
		public decimal BalanceDecimal => this.FetchedBalance?.Amount ?? ServiceRef.TagProfile.LastEDalerBalanceDecimal;

		/// <summary>
		/// Balance formatted with currency.
		/// </summary>
		public string BalanceString => this.BalanceDecimal + " " + this.Currency;

		/// <summary>
		/// Reserved amount.
		/// </summary>
		public decimal ReservedDecimal => this.FetchedBalance?.Reserved ?? -1;

		/// <summary>
		/// Reserved formatted.
		/// </summary>
		public string ReservedString => this.ReservedDecimal == -1
			? ServiceRef.Localizer[nameof(AppResources.UnknownPleaseRefresh)]
			: this.ReservedDecimal + " " + this.Currency;

		/// <summary>
		/// If there is a reserved amount. Only show once balance fetch is complete.
		/// </summary>
		public bool HasReserved => (this.ReservedDecimal > 0 || this.ReservedDecimal == -1) && this.GetBalanceTask.IsSucceeded;

		#endregion

		#region Tasks & Refresh Policy

		/// <summary>
		/// Wraps the balance fetch operation for async UI binding.
		/// </summary>
		public ObservableTask<bool> GetBalanceTask { get; } = new();

		/// <summary>
		/// Loads the token totals per currency, shown in the Tokens section.
		/// </summary>
		public ObservableTask TokenTotalsTask { get; } = new();

		/// <summary>
		/// Loads the most recent tokens, and exposes the loading, error and loaded states of the Tokens section.
		/// </summary>
		public ObservableTask RecentTokensTask { get; } = new();

		#endregion

		#region Tokens

		/// <summary>
		/// If the Tokens section is shown.
		/// </summary>
		public bool AreTokensEnabled => ServiceRef.NeuroWalletOrchestratorService.AreTokensEnabled;

		/// <summary>
		/// Token totals per currency.
		/// </summary>
		public ObservableCollection<TokenTotalItem> TokenTotals { get; } = [];

		/// <summary>
		/// The most recent tokens.
		/// </summary>
		public ObservableCollection<TokenItem> RecentTokens { get; } = [];

		/// <summary>
		/// If there are token totals to show.
		/// </summary>
		public bool HasTokenTotals => this.TokenTotals.Count > 0;

		/// <summary>
		/// If the wallet has no tokens.
		/// </summary>
		public bool HasNoTokens => this.RecentTokens.Count == 0;

		/// <summary>
		/// Text shown when the Tokens section couldn't be loaded.
		/// </summary>
		[ObservableProperty]
		private string? tokensErrorText;

		#endregion

		#region Lifecycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
			ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokensChanged;
			ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokensChanged;
			ServiceRef.TokenUnreadTracker.UnreadChanged += this.TokenUnreadTracker_UnreadChanged;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			// Re-load on appearing.
			this.GetBalanceTask.Load(this.LoadBalanceAsync);

			this.OnPropertyChanged(nameof(this.AreTokensEnabled));

			if (this.AreTokensEnabled)
			{
				if (this.RecentTokensTask.IsSucceeded)
				{
					if (this.lastTokenEvent != ServiceRef.XmppService.LastNeuroFeatureEvent)
						this.ScheduleTokensRefresh();
				}
				else if (!this.RecentTokensTask.IsRunning)
					this.LoadTokens();
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
			ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokensChanged;
			ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokensChanged;
			ServiceRef.TokenUnreadTracker.UnreadChanged -= this.TokenUnreadTracker_UnreadChanged;

			this.tokensRefreshCancel?.Cancel();
			this.TokenTotalsTask.Cancel();
			this.RecentTokensTask.Cancel();

			await base.OnDisposeAsync();
		}

		#endregion

		#region Property Change Logic

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName == nameof(this.IsConnected))
			{
				this.BuyEdalerCommand.NotifyCanExecuteChanged();
				this.SendEdalerCommand.NotifyCanExecuteChanged();

				// A Tokens section that failed while offline loads again once connected.
				if (this.IsConnected && this.AreTokensEnabled && (this.RecentTokensTask.IsFailed || this.RecentTokensTask.IsCanceled))
					this.LoadTokens();
			}
		}

		#endregion

		#region Commands

		/// <summary>
		/// Initiates the Buy eDaler flow. Disabled if not connected.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsConnected))]
		private async Task BuyEdaler()
		{
			try
			{
				IBuyEDalerServiceProvider[] ServiceProviders = await ServiceRef.XmppService.GetServiceProvidersForBuyingEDalerAsync();
				Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();

				// 2. If no providers, navigate to RequestPaymentPage.
				if (ServiceProviders.Length == 0)
				{
					EDalerBalanceNavigationArgs Args = new(Balance);
					await ServiceRef.NavigationService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
					return;
				}

				// 3. Compose list of providers for selection UI.
				List<IBuyEDalerServiceProvider> ServiceProviders2 = [];
				ServiceProviders2.AddRange(ServiceProviders);
				ServiceProviders2.Add(new EmptyBuyEDalerServiceProvider());

				// Todo: Change to ServiceProviders2 IF you want to include from user. We now have a button for "Request" on wallet page.
				ServiceProvidersNavigationArgs SelectionArgs = new(
					ServiceProviders.ToArray(),
					ServiceRef.Localizer[nameof(AppResources.BuyEDaler)],
					ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderBuyEDaler)]
				);

				await ServiceRef.NavigationService.GoToAsync(nameof(ServiceProvidersPage), SelectionArgs, BackMethod.Pop);
				if (SelectionArgs.ServiceProvider is null)
					return;

				IBuyEDalerServiceProvider? ServiceProvider = (IBuyEDalerServiceProvider?)(await SelectionArgs.ServiceProvider.Task);
				if (ServiceProvider is null)
					return;

				// 4. User authentication if required.
				if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.ApplyForOrganizationalId))
					return;

				// 5. No ID: go to RequestPaymentPage.
				if (string.IsNullOrEmpty(ServiceProvider.Id))
				{
					EDalerBalanceNavigationArgs Args = new(Balance);
					await ServiceRef.NavigationService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
				}
				// 6. No contract template: manual amount input flow.
				else if (string.IsNullOrEmpty(ServiceProvider.BuyEDalerTemplateContractId))
				{
					TaskCompletionSource<decimal?> Result = new();
					BuyEDalerNavigationArgs Args = new(Balance?.Currency, Result);

					await ServiceRef.NavigationService.GoToAsync(nameof(BuyEDalerPage), Args, BackMethod.CurrentPage);

					decimal? Amount = await Result.Task;
					if (!Amount.HasValue)
						return;

					if (Amount.Value > 0)
					{
						PaymentTransaction Transaction = await ServiceRef.XmppService.InitiateBuyEDaler(
							ServiceProvider.Id, ServiceProvider.Type, Amount.Value, Balance?.Currency);

						Amount = await Transaction.Wait();

						if (Amount.HasValue && Amount.Value > 0)
						{
						ServiceRef.TagProfile.HasWallet = true;
				}
					}
				}
				// 7. Contract-based buy flow.
				else
				{
					CreationAttributesEventArgs e2 = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
					Dictionary<CaseInsensitiveString, object> Parameters = new()
					{
						{ "Visibility", "CreatorAndParts" },
						{ "Role", "Buyer" },
						{ "Currency", Balance?.Currency ?? e2.Currency },
						{ "TrustProvider", e2.TrustProviderId }
					};

					await ServiceRef.ContractOrchestratorService.OpenContract(
						ServiceProvider.BuyEDalerTemplateContractId,
						ServiceRef.Localizer[nameof(AppResources.BuyEDaler)],
						Parameters
					);

					OptionsTransaction OptionsTransaction =
						await ServiceRef.XmppService.InitiateBuyEDalerGetOptions(ServiceProvider.Id, ServiceProvider.Type);

					IDictionary<CaseInsensitiveString, object>[] Options = await OptionsTransaction.Wait();

					// Show contract options if UI is in a contract page.
					if (ServiceRef.NavigationService.CurrentPage is IContractOptionsPage ContractOptionsPage)
						MainThread.BeginInvokeOnMainThread(async () => await ContractOptionsPage.ShowContractOptions(Options));
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Command to initiate sending eDaler to another user.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsConnected))]
		private async Task SendEdaler()
		{
			try
			{
				// Prepare a base eDaler URI with current currency. Recipient & amount will be set on SendPaymentPage.
				Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();
				StringBuilder Sb = new();
				Sb.Append("edaler:");
				Sb.Append("cu=");
				Sb.Append(Balance.Currency);

				if (!EDalerUri.TryParse(Sb.ToString(), out EDalerUri Parsed))
					return;

				TaskCompletionSource<string?> UriToSend = new();
				TaskCompletionSource<string?> MessageToSend = new();
				EDalerUriNavigationArgs Args = new(Parsed, string.Empty, UriToSend, MessageToSend);
				await ServiceRef.NavigationService.GoToAsync(nameof(SendPaymentPage), Args, BackMethod.Pop);

				string? Uri = await UriToSend.Task; // User composed URI. Null if cancelled.
				string? Message = await MessageToSend.Task; // Optional message.
				if (string.IsNullOrEmpty(Uri))
					return;

				// Automatically claim/process the payment (execute transfer) directly.
				// await ServiceRef.NeuroWalletOrchestratorService.OpenEDalerUri(Uri);
				EDaler.Transaction Transaction = await ServiceRef.XmppService.SendEDalerUri(Uri);

				PaymentSuccessPopup Popup = new(Transaction, Message);
				await ServiceRef.PopupService.PushAsync(Popup);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		[RelayCommand]
		public async Task ViewMainPage()
		{
			try
			{
				if (Application.Current?.MainPage?.Navigation != null)
					await Application.Current.MainPage.Navigation.PopToRootAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		public async Task ViewApps()
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(AppsPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		public async Task ViewRequestPayment()
		{
			try
			{
				EDalerBalanceNavigationArgs Args = new(this.FetchedBalance);
				await ServiceRef.NavigationService.GoToAsync(nameof(RequestPaymentPage), Args);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Creates tokens from a token creation template.
		/// </summary>
		[RelayCommand]
		private Task CreateToken()
		{
			return ServiceRef.NeuroWalletOrchestratorService.CreateTokenAsync();
		}

		/// <summary>
		/// Opens the full list of tokens.
		/// </summary>
		[RelayCommand]
		private async Task SeeAllTokens()
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(MyTokensPage), BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Loads the Tokens section again, after it failed.
		/// </summary>
		[RelayCommand]
		private void ReloadTokens()
		{
			this.LoadTokens();
		}

		/// <summary>
		/// Command to navigate to transaction history page.
		/// </summary>
		[RelayCommand]
		private async Task OpenTransactionHistory()
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(TransactionHistoryPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Loads the current balance from the backend and updates observable properties.
		/// </summary>
		/// <param name="Ctx">Task context</param>
		private async Task LoadBalanceAsync(TaskContext<bool> Ctx)
		{
			ServiceRef.LogService.LogDebug("Refreshing Edaler...");

			if (!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				return;

			Balance CurrentBalance = await ServiceRef.XmppService.GetEDalerBalance();

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.FetchedBalance = CurrentBalance;
				this.BalanceUpdated = DateTime.Now;
				ServiceRef.TagProfile.LastEDalerBalanceDecimal = this.BalanceDecimal;
				ServiceRef.TagProfile.LastEDalerBalanceUpdate = DateTime.Now;
			});

			ServiceRef.LogService.LogDebug("Refreshing Edaler Completed");
		}

		/// <summary>
		/// Loads the Tokens section through its tasks, which show the loading and error states.
		/// </summary>
		private void LoadTokens()
		{
			this.TokenTotalsTask.Load(this.LoadTokenTotalsAsync);
			this.RecentTokensTask.Load(this.LoadRecentTokensAsync);
		}

		/// <summary>
		/// Loads the token totals. When they can't be loaded, the totals chips are left out.
		/// </summary>
		/// <param name="Context">Task context.</param>
		private async Task LoadTokenTotalsAsync(TaskContext<int> Context)
		{
			TokenTotal[] Totals = await FetchTokenTotalsAsync();

			await MainThread.InvokeOnMainThreadAsync(() => this.SetTokenTotals(Totals));
		}

		/// <summary>
		/// Loads the most recent tokens.
		/// </summary>
		/// <param name="Context">Task context.</param>
		private async Task LoadRecentTokensAsync(TaskContext<int> Context)
		{
			try
			{
				this.lastTokenEvent = ServiceRef.XmppService.LastNeuroFeatureEvent;

				Token[] Tokens = await FetchRecentTokensAsync();

				await MainThread.InvokeOnMainThreadAsync(() => this.SetRecentTokens(Tokens));
			}
			catch (Exception Ex)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					this.TokensErrorText = TokenErrors.ToMessage(Ex) ?? ServiceRef.Localizer[nameof(AppResources.TokensLoadFailed)]);
				throw;
			}
		}

		/// <summary>
		/// Waits for the connection to the Neuron.
		/// </summary>
		/// <exception cref="TimeoutException">If the app couldn't connect.</exception>
		private static async Task WaitForTokenConnectionAsync()
		{
			if (!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				throw new TimeoutException();
		}

		private static async Task<TokenTotal[]> FetchTokenTotalsAsync()
		{
			await WaitForTokenConnectionAsync();

			TokenTotalsEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureTotals();
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(e.ErrorText);

			return e.Totals ?? [];
		}

		private static async Task<Token[]> FetchRecentTokensAsync()
		{
			await WaitForTokenConnectionAsync();
			await ServiceRef.TokenUnreadTracker.EnsureLoadedAsync();

			TokensEventArgs e = await ServiceRef.XmppService.GetNeuroFeatures(0, RecentTokenCount);
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(e.ErrorText);

			return e.Tokens ?? [];
		}

		/// <summary>
		/// Shows token totals. Called on the main thread.
		/// </summary>
		/// <param name="Totals">Totals per currency.</param>
		private void SetTokenTotals(TokenTotal[] Totals)
		{
			this.TokenTotals.Clear();

			foreach (TokenTotal Total in Totals)
			{
				if (Total.NrTokens > 0)
					this.TokenTotals.Add(new TokenTotalItem(Total));
			}

			this.OnPropertyChanged(nameof(this.HasTokenTotals));
		}

		/// <summary>
		/// Shows the most recent tokens. Called on the main thread.
		/// </summary>
		/// <param name="Tokens">The tokens, newest first.</param>
		private void SetRecentTokens(Token[] Tokens)
		{
			this.RecentTokens.Clear();

			foreach (Token Token in Tokens)
			{
				ServiceRef.TokenNameCache.Remember(Token);

				this.RecentTokens.Add(new TokenItem(Token)
				{
					UnreadCount = ServiceRef.TokenUnreadTracker.GetUnreadCount(Token.TokenId)
				});
			}

			this.OnPropertyChanged(nameof(this.HasNoTokens));
		}

		/// <summary>
		/// Refreshes the Tokens section shortly, without showing the loading state. Repeated calls are merged.
		/// </summary>
		private void ScheduleTokensRefresh()
		{
			CancellationTokenSource Cancel = new();
			Interlocked.Exchange(ref this.tokensRefreshCancel, Cancel)?.Cancel();

			_ = this.RefreshTokensQuietlyAsync(Cancel.Token);
		}

		private async Task RefreshTokensQuietlyAsync(CancellationToken Cancel)
		{
			try
			{
				await Task.Delay(tokensRefreshDelay, Cancel);

				if (this.RecentTokensTask.IsFailed || this.RecentTokensTask.IsCanceled)
				{
					await MainThread.InvokeOnMainThreadAsync(this.LoadTokens);
					return;
				}

				if (!this.RecentTokensTask.IsSucceeded)
					return;

				this.lastTokenEvent = ServiceRef.XmppService.LastNeuroFeatureEvent;

				Task<TokenTotal[]> TotalsTask = FetchTokenTotalsAsync();
				Token[] Tokens = await FetchRecentTokensAsync();
				TokenTotal[] Totals = await TotalsTask;

				Cancel.ThrowIfCancellationRequested();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.SetRecentTokens(Tokens);
					this.SetTokenTotals(Totals);
				});
			}
			catch (OperationCanceledException)
			{
				// A newer refresh replaced this one.
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private Task Wallet_TokensChanged(object? Sender, TokenEventArgs e)
		{
			if (this.AreTokensEnabled)
				this.ScheduleTokensRefresh();

			return Task.CompletedTask;
		}

		private void TokenUnreadTracker_UnreadChanged(object? Sender, string TokenId)
		{
			int Count = ServiceRef.TokenUnreadTracker.GetUnreadCount(TokenId);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (TokenItem Item in this.RecentTokens)
				{
					if (string.Equals(Item.TokenId, TokenId, StringComparison.OrdinalIgnoreCase))
					{
						Item.UnreadCount = Count;
						break;
					}
				}
			});
		}

		/// <summary>
		/// Handles balance update events from the wallet service.
		/// </summary>
		private async Task Wallet_BalanceUpdated(object? sender, BalanceEventArgs e)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.FetchedBalance = e.Balance;
				this.BalanceUpdated = DateTime.Now;
				ServiceRef.TagProfile.LastEDalerBalanceDecimal = this.BalanceDecimal;
				ServiceRef.TagProfile.LastEDalerBalanceUpdate = DateTime.Now;
			});
		}

		#endregion
	}
}
