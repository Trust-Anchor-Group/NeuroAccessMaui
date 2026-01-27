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
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.UI.Pages.Main.CameraTest; // For EventHandler

namespace NeuroAccessMaui.UI.Pages.Main.Apps
{
	public partial class AppsViewModel : BaseViewModel
	{
		private bool hasBetaFeatures;

		public AppsViewModel() : base()
		{
			this.hasBetaFeatures = ServiceRef.TagProfile.HasBetaFeatures;
		}

		[RelayCommand]
		private static async Task ShowCameraTest()
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(CameraTestPage));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Indicates if beta features are enabled in the profile.
		/// </summary>
		public bool HasBetaFeatures
		{
			get => this.hasBetaFeatures;
			set
			{
				if (this.hasBetaFeatures == value)
					return;
				this.hasBetaFeatures = value;
				this.OnPropertyChanged(nameof(this.HasBetaFeatures));
				this.OnPropertyChanged(nameof(this.BetaButtonStyle));
			}
		}

		/// <summary>
		/// Style for beta‑feature dependent buttons.
		/// </summary>
		public Style BetaButtonStyle => this.HasBetaFeatures ? AppStyles.NeuroIconButton : AppStyles.NeuroIconButtonDisabled;

		[ObservableProperty]
		private bool showBottomNavigation = true;

		/// <summary>
		/// Subscribes to profile property changes.
		/// </summary>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;
		}

		/// <summary>
		/// Unsubscribes from profile property changes.
		/// </summary>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;
			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Handles TagProfile property changes. Updates HasBetaFeatures &amp; BetaButtonStyle if necessary.
		/// </summary>
		private void TagProfile_OnPropertiesChanged(object? sender, EventArgs e)
		{
			bool profileValue = ServiceRef.TagProfile.HasBetaFeatures;
			if (this.HasBetaFeatures != profileValue)
			{
				this.HasBetaFeatures = profileValue;
			}
		}

		#region Navigation Commands

		[RelayCommand]
		private static async Task ShowContacts()
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), BackMethod.Pop);
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
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
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
				await ServiceRef.NavigationService.GoToAsync(nameof(ApplicationsPage));
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
					await ServiceRef.NavigationService.GoToAsync(nameof(MyThingsPage));
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
					await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
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
					await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private async Task ShowMyTokens()
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
					await ServiceRef.NavigationService.GoToAsync(nameof(MyTokensPage), BackMethod.Pop);
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
					await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage));
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
					await ServiceRef.NavigationService.GoToAsync(nameof(WalletPage), Args, BackMethod.Pop);
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
				await ServiceRef.NavigationService.PopToRootAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

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
