using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainPage
	{
		public MainPage(MainViewModel viewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
			this.InitializeTabs();

			Thickness BottomBarPadding = this.BottomBar.Padding;
			BottomBarPadding.Bottom += SafeArea.ResolveInsetsForMode(SafeAreaMode.Bottom).Bottom;
			this.BottomBar.Padding = BottomBarPadding;
		}



		private void InitializeTabs()
		{
			if (this.ContentSwitcher.Views.Count > 0)
			{
				return;
			}

			View homeView = ServiceHelper.GetService<HomePage>();
			View walletView = ServiceHelper.GetService<WalletPage>();
			View appsView = ServiceHelper.GetService<AppsPage>();

			if (walletView.BindingContext is WalletViewModel walletViewModel)
			{
				walletViewModel.ShowBottomNavigation = false;
			}

			if (appsView.BindingContext is AppsViewModel appsViewModel)
			{
				appsViewModel.ShowBottomNavigation = false;
			}

			this.ContentSwitcher.Views.Add(homeView);
			this.ContentSwitcher.Views.Add(walletView);
			this.ContentSwitcher.Views.Add(appsView);
		}
	}
}
