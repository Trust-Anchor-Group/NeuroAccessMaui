using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using EDaler.Events;
using EDaler.Uris;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Wallet;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.RequestPayment;
using NeuroAccessMaui.UI.Pages.Wallet.SellEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroFeatures.EventArguments;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// The view model to bind to for when displaying the wallet.
	/// </summary>
	/// <param name="Args">Navigation arguments</param>
	public partial class MyWalletViewModel(WalletNavigationArgs? Args) : XmppViewModel()
	{
		private readonly WalletNavigationArgs? navigationArguments = Args;
		private DateTime lastEDalerEvent;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			this.EDalerFrontGlyph = "https://" + ServiceRef.TagProfile.Domain + "/Images/eDalerFront200.png";

			if (this.navigationArguments is not null)
			{
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents = this.GetNotificationEvents();

				await this.AssignProperties(this.navigationArguments.Balance, this.navigationArguments.PendingAmount,
					this.navigationArguments.PendingCurrency, this.navigationArguments.PendingPayments, this.navigationArguments.Events,
					this.navigationArguments.More, ServiceRef.XmppService.LastEDalerEvent, NotificationEvents);
			}

			ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
			ServiceRef.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (((this.Balance is not null) && (ServiceRef.XmppService.LastEDalerBalance is not null) &&
				(this.Balance.Amount != ServiceRef.XmppService.LastEDalerBalance.Amount ||
				this.Balance.Currency != ServiceRef.XmppService.LastEDalerBalance.Currency ||
				this.Balance.Timestamp != ServiceRef.XmppService.LastEDalerBalance.Timestamp)) ||
				this.lastEDalerEvent != ServiceRef.XmppService.LastEDalerEvent)
			{
				await this.ReloadEDalerWallet(ServiceRef.XmppService.LastEDalerBalance ?? this.Balance);
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
			ServiceRef.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;

			await base.OnDisposeAsync();
		}

		private SortedDictionary<CaseInsensitiveString, NotificationEvent[]> GetNotificationEvents()
		{
			SortedDictionary<CaseInsensitiveString, NotificationEvent[]> Result = ServiceRef.NotificationService.GetEventsByCategory(NotificationEventType.Wallet);
			int NrBalance = 0;

			foreach (NotificationEvent[] Events in Result.Values)
			{
				foreach (NotificationEvent Event in Events)
				{
					if (Event is BalanceNotificationEvent)
						NrBalance++;
				}
			}

			this.NrBalanceNotifications = NrBalance;

			return Result;
		}

		private async Task AssignProperties(Balance? Balance, decimal PendingAmount, string? PendingCurrency,
			EDaler.PendingPayment[]? PendingPayments, EDaler.AccountEvent[]? Events, bool More, DateTime LastEvent,
			SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents)
		{
			if (Balance is not null)
			{
				this.Balance = Balance;
				this.Amount = Balance.Amount;
				this.ReservedAmount = Balance.Reserved;
				this.Currency = Balance.Currency;
				this.Timestamp = Balance.Timestamp;
			}

			this.lastEDalerEvent = LastEvent;

			this.PendingAmount = PendingAmount;
			this.PendingCurrency = PendingCurrency;
			this.HasPending = (PendingPayments?.Length ?? 0) > 0;
			this.HasEvents = (Events?.Length ?? 0) > 0;
			this.HasMoreEvents = More;

			Dictionary<string, string> FriendlyNames = [];
			string? FriendlyName;

			ObservableItemGroup<IUniqueItem> NewPaymentItems = new(nameof(this.PaymentItems), []);

			if (PendingPayments is not null)
			{
				List<IUniqueItem> NewPendingPayments = new(PendingPayments.Length);

				foreach (EDaler.PendingPayment Payment in PendingPayments)
				{
					if (!FriendlyNames.TryGetValue(Payment.To, out FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Payment.To);
						FriendlyNames[Payment.To] = FriendlyName;
					}

					NewPendingPayments.Add(new PendingPaymentItem(Payment, FriendlyName));
				}

				if (NewPendingPayments.Count > 0)
					NewPaymentItems.Add(new ObservableItemGroup<IUniqueItem>(nameof(PendingPaymentItem), NewPendingPayments));
			}

			if (Events is not null)
			{
				List<IUniqueItem> NewAccountEvents = new(Events.Length);

				foreach (EDaler.AccountEvent Event in Events)
				{
					if (!FriendlyNames.TryGetValue(Event.Remote, out FriendlyName))
					{
						FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote);
						FriendlyNames[Event.Remote] = FriendlyName;
					}

					if (!NotificationEvents.TryGetValue(Event.TransactionId.ToString(), out NotificationEvent[]? CategoryEvents))
						CategoryEvents = [];

					NewAccountEvents.Add(new AccountEventItem(Event, this, FriendlyName, CategoryEvents));
				}

				if (NewAccountEvents.Count > 0)
					NewPaymentItems.Add(new ObservableItemGroup<IUniqueItem>(nameof(AccountEventItem), NewAccountEvents));
			}

			MainThread.BeginInvokeOnMainThread(() => ObservableItemGroup<IUniqueItem>.UpdateGroupsItems(this.PaymentItems, NewPaymentItems));
		}

		private Task Wallet_BalanceUpdated(object? Sender, BalanceEventArgs e)
		{
			Task.Run(() => this.ReloadEDalerWallet(e.Balance));
			return Task.CompletedTask;
		}

		private async Task ReloadEDalerWallet(Balance? Balance)
		{
			try
			{
				(decimal PendingAmount, string PendingCurrency, EDaler.PendingPayment[] PendingPayments) = await ServiceRef.XmppService.GetPendingEDalerPayments();
				(EDaler.AccountEvent[] Events, bool More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);
				IUniqueItem? OldItems = this.PaymentItems.FirstOrDefault(el => string.Equals(el.UniqueName, nameof(AccountEventItem), StringComparison.Ordinal));

				// Reload also items which were loaded earlier by the LoadMoreAccountEvents
				if (More &&
					(OldItems is ObservableItemGroup<IUniqueItem> OldAccountEvents) &&
					(OldAccountEvents.LastOrDefault() is AccountEventItem OldLastEvent) &&
					(Events.LastOrDefault() is EDaler.AccountEvent NewLastEvent) &&
					(OldLastEvent.Timestamp < NewLastEvent.Timestamp))
				{
					List<EDaler.AccountEvent> AllEvents = new(Events);
					EDaler.AccountEvent[] Events2;
					bool More2 = true;

					while (More2)
					{
						EDaler.AccountEvent LastEvent = AllEvents.Last();
						(Events2, More2) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize, LastEvent.Timestamp);

						if (More2)
						{
							More = true;

							for (int i = 0; i < Events2.Length; i++)
							{
								EDaler.AccountEvent Event = Events2[i];
								AllEvents.Add(Event);

								if (OldLastEvent.Timestamp.Equals(Event.Timestamp))
								{
									More2 = false;
									break;
								}
							}
						}
						else
						{
							More = false;
							AllEvents.AddRange(Events2);
						}
					}

					Events = [.. AllEvents];
				}

				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents = this.GetNotificationEvents();

				MainThread.BeginInvokeOnMainThread(async () => await this.AssignProperties(Balance, PendingAmount, PendingCurrency,
					PendingPayments, Events, More, ServiceRef.XmppService.LastEDalerEvent, NotificationEvents));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		#region Properties

		/// <summary>
		/// Balance of eDaler to process
		/// </summary>
		[ObservableProperty]
		private Balance? balance;

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal amount;

		/// <summary>
		/// Currency of eDaler to process
		/// </summary>
		[ObservableProperty]
		private string? currency;

		/// <summary>
		/// HasPending of eDaler to process
		/// </summary>
		[ObservableProperty]
		private bool hasPending;

		/// <summary>
		/// PendingAmount of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal pendingAmount;

		/// <summary>
		/// PendingCurrency of eDaler to process
		/// </summary>
		[ObservableProperty]
		private string? pendingCurrency;

		/// <summary>
		/// ReservedAmount of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal reservedAmount;

		/// <summary>
		/// When code was created.
		/// </summary>
		[ObservableProperty]
		private DateTime timestamp;

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		[ObservableProperty]
		private string? eDalerFrontGlyph;

		/// <summary>
		/// HasEvents of eDaler to process
		/// </summary>
		[ObservableProperty]
		private bool hasEvents;

		/// <summary>
		/// If there are more account events available.
		/// </summary>
		[ObservableProperty]
		private bool hasMoreEvents;

		/// <summary>
		/// When last eDaler event was received.
		/// </summary>
		[ObservableProperty]
		private int nrBalanceNotifications;

		/// <summary>
		/// Holds pending payments and account events. Both are also observable collections.
		/// </summary>
		public ObservableItemGroup<IUniqueItem> PaymentItems { get; } = new(nameof(PaymentItems), []);

		#endregion

		/// <summary>
		/// The command to bind to for returning to previous view.
		/// </summary>
		[RelayCommand]
		private Task Back()
		{
			return this.GoBack();
		}

		/// <summary>
		/// The command to bind to for allowing users to scan QR codes.
		/// </summary>
		[RelayCommand]
		private static async Task ScanQrCode()
		{
			await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult();
		}

		/// <summary>
		/// The command to bind to for requesting a payment
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsConnected))]
		private async Task RequestPayment()
		{
			try
			{
				IBuyEDalerServiceProvider[] ServiceProviders = await ServiceRef.XmppService.GetServiceProvidersForBuyingEDalerAsync();

				if (ServiceProviders.Length == 0)
				{
					EDalerBalanceNavigationArgs Args = new(this.Balance);

					await ServiceRef.NavigationService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
				}
				else
				{
					List<IBuyEDalerServiceProvider> ServiceProviders2 = [];

					ServiceProviders2.AddRange(ServiceProviders);
					ServiceProviders2.Add(new EmptyBuyEDalerServiceProvider());

					ServiceProvidersNavigationArgs e = new(ServiceProviders2.ToArray(),
						ServiceRef.Localizer[nameof(AppResources.BuyEDaler)],
						ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderBuyEDaler)]);

					await ServiceRef.NavigationService.GoToAsync(nameof(ServiceProvidersPage), e, BackMethod.Pop);

					IBuyEDalerServiceProvider? ServiceProvider = (IBuyEDalerServiceProvider?)(e.ServiceProvider is null ? null : await e.ServiceProvider.Task);

					if (ServiceProvider is not null)
					{
						if (string.IsNullOrEmpty(ServiceProvider.Id))
						{
							EDalerBalanceNavigationArgs Args = new(this.Balance);

							await ServiceRef.NavigationService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
						}
						else if (string.IsNullOrEmpty(ServiceProvider.BuyEDalerTemplateContractId))
						{
							TaskCompletionSource<decimal?> Result = new();
							BuyEDalerNavigationArgs Args = new(this.Balance?.Currency, Result);

							await ServiceRef.NavigationService.GoToAsync(nameof(BuyEDalerPage), Args, BackMethod.CurrentPage);

							decimal? Amount = await Result.Task;

							if (Amount.HasValue && Amount.Value > 0)
							{
								PaymentTransaction Transaction = await ServiceRef.XmppService.InitiateBuyEDaler(ServiceProvider.Id, ServiceProvider.Type,
									Amount.Value, this.Balance?.Currency);

								WaitForComletion(Transaction);
							}
						}
						else
						{
							CreationAttributesEventArgs e2 = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
							Dictionary<CaseInsensitiveString, object> Parameters = new()
							{
								{ "Visibility", "CreatorAndParts" },
								{ "Role", "Buyer" },
								{ "Currency", this.Balance?.Currency ?? e2.Currency },
								{ "TrustProvider", e2.TrustProviderId }
							};

							await ServiceRef.ContractOrchestratorService.OpenContract(ServiceProvider.BuyEDalerTemplateContractId,
								ServiceRef.Localizer[nameof(AppResources.BuyEDaler)], Parameters);

							OptionsTransaction OptionsTransaction = await ServiceRef.XmppService.InitiateBuyEDalerGetOptions(ServiceProvider.Id, ServiceProvider.Type);
							IDictionary<CaseInsensitiveString, object>[] Options = await OptionsTransaction.Wait();

							if (ServiceRef.NavigationService.CurrentPage is IContractOptionsPage ContractOptionsPage)
								MainThread.BeginInvokeOnMainThread(async () => await ContractOptionsPage.ShowContractOptions(Options));
						}
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private static async void WaitForComletion(PaymentTransaction Transaction)
		{
			try
			{
				await Transaction.Wait();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// The command to bind to for making a payment
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsConnected))]
		private async Task MakePayment()
		{
			try
			{
				ISellEDalerServiceProvider[] ServiceProviders = await ServiceRef.XmppService.GetServiceProvidersForSellingEDalerAsync();

				if (ServiceProviders.Length == 0)
				{
					ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.SelectContactToPay)], SelectContactAction.MakePayment)
					{
						CanScanQrCode = true,
						AllowAnonymous = true,
						AnonymousText = ServiceRef.Localizer[nameof(AppResources.Open)]
					};

					await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.CurrentPage);
				}
				else
				{
					List<ISellEDalerServiceProvider> ServiceProviders2 = [];

					ServiceProviders2.AddRange(ServiceProviders);
					ServiceProviders2.Add(new EmptySellEDalerServiceProvider());

					ServiceProvidersNavigationArgs e = new(ServiceProviders2.ToArray(),
						ServiceRef.Localizer[nameof(AppResources.SellEDaler)],
						ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderSellEDaler)]);

					await ServiceRef.NavigationService.GoToAsync(nameof(ServiceProvidersPage), e, BackMethod.Pop);

					ISellEDalerServiceProvider? ServiceProvider = (ISellEDalerServiceProvider?)(e.ServiceProvider is null ? null : await e.ServiceProvider.Task);

					if (ServiceProvider is not null)
					{
						if (string.IsNullOrEmpty(ServiceProvider.Id))
						{
							ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.SelectContactToPay)], SelectContactAction.MakePayment)
							{
								CanScanQrCode = true,
								AllowAnonymous = true,
								AnonymousText = ServiceRef.Localizer[nameof(AppResources.Open)],
							};

							await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.CurrentPage);
						}
						else if (string.IsNullOrEmpty(ServiceProvider.SellEDalerTemplateContractId))
						{
							TaskCompletionSource<decimal?> Result = new();
							SellEDalerNavigationArgs Args = new(this.Balance?.Currency, Result);

							await ServiceRef.NavigationService.GoToAsync(nameof(SellEDalerPage), Args, BackMethod.CurrentPage);

							decimal? Amount = await Result.Task;

							if (Amount.HasValue && Amount.Value > 0)
							{
								PaymentTransaction Transaction = await ServiceRef.XmppService.InitiateSellEDaler(ServiceProvider.Id, ServiceProvider.Type,
									Amount.Value, this.Balance?.Currency);

								WaitForComletion(Transaction);
							}
						}
						else
						{
							CreationAttributesEventArgs e2 = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
							Dictionary<CaseInsensitiveString, object> Parameters = new()
							{
								{ "Visibility", "CreatorAndParts" },
								{ "Role", "Seller" },
								{ "Currency", this.Balance?.Currency ?? e2.Currency },
								{ "TrustProvider", e2.TrustProviderId }
							};

							await ServiceRef.ContractOrchestratorService.OpenContract(ServiceProvider.SellEDalerTemplateContractId,
								ServiceRef.Localizer[nameof(AppResources.SellEDaler)], Parameters);

							OptionsTransaction OptionsTransaction = await ServiceRef.XmppService.InitiateSellEDalerGetOptions(ServiceProvider.Id, ServiceProvider.Type);
							IDictionary<CaseInsensitiveString, object>[] Options = await OptionsTransaction.Wait();

							if (ServiceRef.NavigationService.CurrentPage is IContractOptionsPage ContractOptionsPage)
								MainThread.BeginInvokeOnMainThread(async () => await ContractOptionsPage.ShowContractOptions(Options));
						}
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// The command to bind to for displaying information about a pending payment or an account event.
		/// </summary>
		[RelayCommand]
		private async Task ShowPaymentItem(object Item)
		{
			if (Item is PendingPaymentItem PendingItem)
			{
				if (!ServiceRef.XmppService.TryParseEDalerUri(PendingItem.Uri, out EDalerUri Uri, out string Reason))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.InvalidEDalerUri), Reason]);
					return;
				}

				await ServiceRef.NavigationService.GoToAsync(nameof(PendingPayment.PendingPaymentPage), new EDalerUriNavigationArgs(Uri, PendingItem.FriendlyName));
			}
			else if (Item is AccountEventItem EventItem)
				await ServiceRef.NavigationService.GoToAsync(nameof(AccountEvent.AccountEventPage), new AccountEvent.AccountEventNavigationArgs(EventItem));
		}

		/// <summary>
		/// Command executed when more account events need to be displayed.
		/// </summary>
		[RelayCommand]
		private async Task LoadMoreAccountEvents()
		{
			if (this.HasMoreEvents)
			{
				this.HasMoreEvents = false; // So multiple requests are not made while scrolling.
				bool More = true;

				try
				{
					EDaler.AccountEvent[]? Events = null;
					IUniqueItem? OldItems = this.PaymentItems.FirstOrDefault(el => string.Equals(el.UniqueName, nameof(AccountEventItem), StringComparison.Ordinal));

					if (OldItems is null)
						(Events, More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);
					else
					{
						ObservableItemGroup<IUniqueItem> OldAccountEvents = (ObservableItemGroup<IUniqueItem>)OldItems;

						if (OldAccountEvents.LastOrDefault() is AccountEventItem LastEvent)
						{
							(Events, More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize, LastEvent.Timestamp);
						}
					}

					if (Events is not null)
					{
						List<IUniqueItem> NewAccountEvents = [];
						Dictionary<string, string> FriendlyNames = [];
						SortedDictionary<CaseInsensitiveString, NotificationEvent[]> NotificationEvents = this.GetNotificationEvents();

						foreach (EDaler.AccountEvent Event in Events)
						{
							if (!FriendlyNames.TryGetValue(Event.Remote, out string? FriendlyName))
							{
								FriendlyName = await ContactInfo.GetFriendlyName(Event.Remote);
								FriendlyNames[Event.Remote] = FriendlyName;
							}

							if (!NotificationEvents.TryGetValue(Event.TransactionId.ToString(), out NotificationEvent[]? CategoryEvents))
								CategoryEvents = [];

							NewAccountEvents.Add(new AccountEventItem(Event, this, FriendlyName, CategoryEvents));
						}

						MainThread.BeginInvokeOnMainThread(() =>
						{

							if (OldItems is ObservableItemGroup<IUniqueItem> SubItems)
							{
								foreach (IUniqueItem Item in NewAccountEvents)
									SubItems.Add(Item);
							}
							else
							{
								this.PaymentItems.Add(new ObservableItemGroup<IUniqueItem>(nameof(AccountEventItem), NewAccountEvents));
								this.HasMoreEvents = More;
							}
						});
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}
		}

		private Task NotificationService_OnNewNotification(object? Sender, NotificationEventArgs e)
		{
			if (e.Event is BalanceNotificationEvent)
				MainThread.BeginInvokeOnMainThread(() => this.NrBalanceNotifications++);

			return Task.CompletedTask;
		}

		// Go to Apps page
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
		public async Task ViewMainPage()
		{
			try
			{
				await ServiceRef.NavigationService.PopToRootAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
