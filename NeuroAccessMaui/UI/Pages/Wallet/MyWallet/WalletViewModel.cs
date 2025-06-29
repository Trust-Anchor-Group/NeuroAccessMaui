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
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.RequestPayment;
using NeuroAccessMaui.UI.Pages.Wallet.SellEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using System.ComponentModel;
using System.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script.Constants;
using Waher.Script.Functions.ComplexNumbers;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// The view model to bind to for when displaying the wallet.
	/// </summary>
	public partial class WalletViewModel() : XmppViewModel()
	{

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(BalanceDecimal))]
		Balance? fetchedBalance;

		[ObservableProperty]
		private bool isRefreshingBalance;

		public decimal BalanceDecimal => this.FetchedBalance?.Amount ?? ServiceRef.TagProfile.LastEDalerBalanceDecimal;

		[ObservableProperty]
		private DateTime? balanceUpdated = ServiceRef.TagProfile.LastEDalerBalanceUpdate;


		public ObservableTask<bool> GetBalanceTask { get; } = new();







		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.GetBalanceTask.Load(this.LoadBalanceAsync);


			//	ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
			//	ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
			//	ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			//	ServiceRef.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
		}

		/// <inheritdoc/>
		protected override async Task OnAppearing()
		{
			await base.OnAppearing();
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			//	ServiceRef.XmppService.EDalerBalanceUpdated -= this.Wallet_BalanceUpdated;
			//	ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
			//	ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
			//	ServiceRef.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;

			await base.OnDispose();
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName == nameof(this.IsConnected))
				this.BuyEdalerCommand.NotifyCanExecuteChanged();
		}

		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsConnected))]
		private async Task BuyEdaler()
		{
			try
			{
				IBuyEDalerServiceProvider[] ServiceProviders = await ServiceRef.XmppService.GetServiceProvidersForBuyingEDalerAsync();
				Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();

				if (ServiceProviders.Length == 0)
				{
					EDalerBalanceNavigationArgs Args = new(Balance);
					await ServiceRef.UiService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
				}
				else
				{
					List<IBuyEDalerServiceProvider> ServiceProviders2 = [];

					ServiceProviders2.AddRange(ServiceProviders);
					ServiceProviders2.Add(new EmptyBuyEDalerServiceProvider());

					ServiceProvidersNavigationArgs e = new(ServiceProviders2.ToArray(),
						ServiceRef.Localizer[nameof(AppResources.BuyEDaler)],
						ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderBuyEDaler)]);

					await ServiceRef.UiService.GoToAsync(nameof(ServiceProvidersPage), e, BackMethod.Pop);
					if (e.ServiceProvider is null)
						return;

					IBuyEDalerServiceProvider? ServiceProvider = (IBuyEDalerServiceProvider?)(await e.ServiceProvider.Task);
					if (ServiceProvider is null)
						return;

					if (!await App.AuthenticateUserAsync(AuthenticationPurpose.ApplyForOrganizationalId))
						return;

					if (string.IsNullOrEmpty(ServiceProvider.Id))
					{
						EDalerBalanceNavigationArgs Args = new(Balance);
						await ServiceRef.UiService.GoToAsync(nameof(RequestPaymentPage), Args, BackMethod.CurrentPage);
					}
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
							PaymentTransaction Transaction = await ServiceRef.XmppService.InitiateBuyEDaler(ServiceProvider.Id, ServiceProvider.Type,
								Amount.Value, Balance?.Currency);

							Amount = await Transaction.Wait();

							if (Amount.HasValue && Amount.Value > 0)
							{
								ServiceRef.TagProfile.HasWallet = true;
							}
						}
					}
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

						await ServiceRef.ContractOrchestratorService.OpenContract(ServiceProvider.BuyEDalerTemplateContractId,
							ServiceRef.Localizer[nameof(AppResources.BuyEDaler)], Parameters);

						OptionsTransaction OptionsTransaction = await ServiceRef.XmppService.InitiateBuyEDalerGetOptions(ServiceProvider.Id, ServiceProvider.Type);
						IDictionary<CaseInsensitiveString, object>[] Options = await OptionsTransaction.Wait();

						if (ServiceRef.UiService.CurrentPage is IContractOptionsPage ContractOptionsPage)
							MainThread.BeginInvokeOnMainThread(async () => await ContractOptionsPage.ShowContractOptions(Options));
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}


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

		private async Task LoadAccountEventsAsync(TaskContext<bool> Ctx)
		{
			if (!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				return;

		}
	}

}
