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
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.RequestPayment;
using NeuroAccessMaui.UI.Pages.Wallet.SellEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.SendPayment;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script.Constants;
using Waher.Script.Functions.ComplexNumbers;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// ViewModel for the Wallet page, handling balance display, buy flow, and related navigation.
	/// </summary>
	public partial class WalletViewModel() : XmppViewModel()
	{
		#region Observable Properties

		/// <summary>
		/// Last fetched balance. Changing this notifies <see cref="BalanceDecimal"/>.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(BalanceDecimal))]
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

		#endregion

		#region Computed Properties

		/// <summary>
		/// Exposes the current balance as a decimal.
		/// </summary>
		public decimal BalanceDecimal =>
			this.FetchedBalance?.Amount ?? ServiceRef.TagProfile.LastEDalerBalanceDecimal;

		public string BalanceString =>
			this.BalanceDecimal + " NC";

		#endregion

		#region Tasks

		/// <summary>
		/// Wraps the balance fetch operation for async UI binding.
		/// </summary>
		public ObservableTask<bool> GetBalanceTask { get; } = new();

		#endregion

		#region Lifecycle

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			this.GetBalanceTask.Load(this.LoadBalanceAsync);

			// Uncomment if event handlers are needed
			// ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
			// ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
			// ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			// ServiceRef.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
		}

		/// <inheritdoc/>
		protected override async Task OnAppearing()
		{
			await base.OnAppearing();
			// Place for page appearing logic if needed.
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			// Uncomment if event handlers are subscribed in OnInitialize.
			// ServiceRef.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
			// ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
			// ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
			// ServiceRef.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;

			await base.OnDispose();
		}

		#endregion

		#region Property Change Logic

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			// Update command states as appropriate
			if (e.PropertyName == nameof(this.IsConnected))
			{
				this.BuyEdalerCommand.NotifyCanExecuteChanged();
				this.SendEdalerCommand.NotifyCanExecuteChanged();
			}
		}

		#endregion

		#region Commands

		/// <summary>
		/// Initiates the Buy eDaler flow. Disabled if not connected.
		/// </summary>
		/// <returns></returns>
		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsConnected))]
		private async Task BuyEdaler()
		{
			try
			{
				// 1. Fetch available service providers and the current balance.
				IBuyEDalerServiceProvider[] ServiceProviders = await ServiceRef.XmppService.GetServiceProvidersForBuyingEDalerAsync();
				Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();

				// 2. If no providers, navigate to RequestPaymentPage.
				if (ServiceProviders.Length == 0)
				{
					EDalerBalanceNavigationArgs Args = new(Balance);
					await ServiceRef.UiService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
					return;
				}

				// 3. Compose list of providers for selection UI.
				List<IBuyEDalerServiceProvider> ServiceProviders2 = [];
				ServiceProviders2.AddRange(ServiceProviders);
				ServiceProviders2.Add(new EmptyBuyEDalerServiceProvider());

				ServiceProvidersNavigationArgs e = new(
					ServiceProviders2.ToArray(),
					ServiceRef.Localizer[nameof(AppResources.BuyEDaler)],
					ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderBuyEDaler)]
				);

				await ServiceRef.UiService.GoToAsync(nameof(ServiceProvidersPage), e, BackMethod.Pop);
				if (e.ServiceProvider is null)
					return;

				IBuyEDalerServiceProvider? ServiceProvider = (IBuyEDalerServiceProvider?)(await e.ServiceProvider.Task);
				if (ServiceProvider is null)
					return;

				// 4. User authentication if required.
				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.ApplyForOrganizationalId))
					return;

				// 5. No ID: go to RequestPaymentPage.
				if (string.IsNullOrEmpty(ServiceProvider.Id))
				{
					EDalerBalanceNavigationArgs Args = new(Balance);
					await ServiceRef.UiService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
				}
				// 6. No contract template: manual amount input flow.
				else if (string.IsNullOrEmpty(ServiceProvider.BuyEDalerTemplateContractId))
				{
					TaskCompletionSource<decimal?> Result = new();
					BuyEDalerNavigationArgs Args = new(Balance?.Currency, Result);

					await ServiceRef.UiService.GoToAsync(nameof(BuyEDalerPage), Args, BackMethod.CurrentPage);

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
					if (ServiceRef.UiService.CurrentPage is IContractOptionsPage ContractOptionsPage)
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
				EDalerUriNavigationArgs Args = new(Parsed, string.Empty, UriToSend);
				await ServiceRef.UiService.GoToAsync(nameof(SendPaymentPage), Args, BackMethod.Pop);

				string? Uri = await UriToSend.Task; // User composed URI. Null if cancelled.
				if (string.IsNullOrEmpty(Uri))
					return;

				// Automatically claim/process the payment (execute transfer) directly.
				// await ServiceRef.NeuroWalletOrchestratorService.OpenEDalerUri(Uri);
				await ServiceRef.XmppService.SendEDalerUri(Uri);

				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.PaymentSuccess)]);
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
				{
					await Application.Current.MainPage.Navigation.PopToRootAsync();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		// Go to Apps page
		[RelayCommand]
		public async Task ViewApps()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(AppsPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
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
				this.BalanceUpdated = DateTime.UtcNow;
				ServiceRef.TagProfile.LastEDalerBalanceDecimal = this.BalanceDecimal;
				ServiceRef.TagProfile.LastEDalerBalanceUpdate = DateTime.UtcNow;
			});

			ServiceRef.LogService.LogDebug("Refreshing Edaler Completed");
		}

		/// <summary>
		/// Loads account event history (not yet implemented).
		/// </summary>
		/// <param name="Ctx">Task context</param>
		private async Task LoadAccountEventsAsync(TaskContext<bool> Ctx)
		{
			if (!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				return;

		}
		#endregion
	}
}
