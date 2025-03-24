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

		Balance? lastBalance;

		[ObservableProperty]
		private bool isRefreshingBalance;

		public decimal BalanceDecimal => this.LastBalance?.Amount ?? 0;

		public DateTime? BalanceUpdatedTimestamp => this.LastBalance?.Timestamp;






		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();


			//	ServiceRef.XmppService.EDalerBalanceUpdated += this.Wallet_BalanceUpdated;
			//	ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
			//	ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			//	ServiceRef.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
		}

		/// <inheritdoc/>
		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			this.LastBalance = ServiceRef.XmppService.LastEDalerBalance;

			if (this.RefreshBalanceCommand.CanExecute(null))
			{
				await this.RefreshBalanceCommand.ExecuteAsync(null);
			}




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

		[RelayCommand(AllowConcurrentExecutions = false)]
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
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}


		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RefreshBalanceAsync()
		{
			try
			{
				ServiceRef.LogService.LogDebug("Refreshing Edaler...");
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsRefreshingBalance = true;
				});

				await Task.Delay(500);

				Balance CurrentBalance = await ServiceRef.XmppService.GetEDalerBalance();

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.LastBalance = CurrentBalance;
				});
				ServiceRef.LogService.LogDebug("Refreshing Edaler Completed");

			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsRefreshingBalance = false;
				});
			}
		}

	}

}
