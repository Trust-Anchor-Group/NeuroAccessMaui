
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using EDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.Resources.Languages;
using System.Runtime.CompilerServices;
using NeuroAccessMaui.Services.Authentication;

namespace NeuroAccessMaui.UI.Pages.Main.Apps
{
    public partial class AppsViewModel : BaseViewModel
	{
		private bool hasBetaFeatures;

		public AppsViewModel() : base()
		{
			this.hasBetaFeatures = ServiceRef.TagProfile.HasBetaFeatures;
		}

		public bool HasBetaFeatures
		{
			get => this.hasBetaFeatures;
			set => this.hasBetaFeatures = value;
		}

		// Binding for selecting between NeuroIconButton and NeuroIconButtonDisabled style depending on if has beta features enabled
		public Style BetaButtonStyle => this.HasBetaFeatures ? AppStyles.NeuroIconButton : AppStyles.NeuroIconButtonDisabled;

		#region Navigation Commands

		[RelayCommand]
		private static async Task ShowContacts()
		{
			try
			{
				ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.ContactsDescription)], SelectContactAction.ViewIdentity);
				await ServiceRef.UiService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private static async Task ShowMyContracts()
		{
			try
			{
				MyContractsNavigationArgs Args = new(ContractsListMode.Contracts);
				await ServiceRef.UiService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		[RelayCommand]
		private static async Task ShowApplications()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(ApplicationsPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[ObservableProperty]
		private bool betaFeaturePressed = false;

		[RelayCommand]
		private async Task ShowThings()
		{
			try
			{
				if(!ServiceRef.TagProfile.HasBetaFeatures)
				{
					this.BetaFeaturePressed = true;
					await Task.Delay(100);
					this.BetaFeaturePressed = false;
				}
				else
				{
					await ServiceRef.UiService.GoToAsync(nameof(MyThingsPage));
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private async Task ShowNewContract()
		{
			try
			{
				if (!ServiceRef.TagProfile.HasBetaFeatures)
				{
					this.BetaFeaturePressed = true;
					await Task.Delay(100);
					this.BetaFeaturePressed = false;
				}
				else
				{
					MyContractsNavigationArgs Args = new(ContractsListMode.ContractTemplates);
					await ServiceRef.UiService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private async Task ShowNewToken()
		{
			try
			{
				if (!ServiceRef.TagProfile.HasBetaFeatures)
				{
					this.BetaFeaturePressed = true;
					await Task.Delay(100);
					this.BetaFeaturePressed = false;
				}
				else
				{
					MyContractsNavigationArgs Args = new(ContractsListMode.TokenCreationTemplates);
					await ServiceRef.UiService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private static async Task ShowId()
		{
			try
			{
				if (await ServiceRef.Provider.GetRequiredService<IAuthenticationService>().AuthenticateUserAsync(AuthenticationPurpose.ViewId))
					await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		public async Task ShowWallet()
		{
			try
			{
				if (!ServiceRef.TagProfile.HasBetaFeatures)
				{
					this.BetaFeaturePressed = true;
					await Task.Delay(100);
					this.BetaFeaturePressed = false;
				}
				else
				{
					WalletNavigationArgs Args = new();
					await ServiceRef.UiService.GoToAsync(nameof(WalletPage), Args, BackMethod.Pop);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		[RelayCommand]
		private async Task ShowWalletBottombar()
		{
			if (!ServiceRef.TagProfile.HasBetaFeatures)
			{
				await this.ShowComingSoonPopup();
			}
			else
			{
				await this.ShowWallet();
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

		// Code used for displayig the coming soon popup

		[ObservableProperty]
		private bool showingComingSoonPopup = false;

		[RelayCommand(AllowConcurrentExecutions = false)]
		public async Task ShowComingSoonPopup()
		{
			this.ShowingComingSoonPopup = true;
			await Task.Delay(3000);
			this.ShowingComingSoonPopup = false;
		}

		#endregion
	}
}
