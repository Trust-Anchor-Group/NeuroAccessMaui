using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using System.Linq;

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

		private bool isFirstAppear = true;
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			try
			{
				this.ApplyNavigationArgs();

				if (this.ContentSwitcher is not null && !this.isFirstAppear)
					await this.ContentSwitcher.NotifyHostAppearingAsync();
				else
					this.isFirstAppear = false;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		public override async Task OnDisappearingAsync()
		{
			try
			{
				if (this.ContentSwitcher is not null)
					await this.ContentSwitcher.NotifyHostDisappearingAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				await base.OnDisappearingAsync();
			}
		}



		private void InitializeTabs()
		{
			if (this.ContentSwitcher.Views.Count >= 3)
			{
				this.EnsureSelectedTabIndex();
				return;
			}

			this.ContentSwitcher.Views.Clear();

			View HomeView = this.ResolveView<HomePage>("home");
			View WalletView = this.ResolveView<WalletPage>("wallet");
			View AppsView = this.ResolveView<AppsPage>("apps");

			if (WalletView.BindingContext is WalletViewModel WalletViewModel)
			{
				WalletViewModel.ShowBottomNavigation = false;
			}

			if (AppsView.BindingContext is AppsViewModel AppsViewModel)
			{
				AppsViewModel.ShowBottomNavigation = false;
			}

			this.ContentSwitcher.Views.Add(HomeView);
			this.ContentSwitcher.Views.Add(WalletView);
			this.ContentSwitcher.Views.Add(AppsView);

			this.EnsureSelectedTabIndex();
		}

		private void ApplyNavigationArgs()
		{
			MainNavigationArgs? args = ServiceRef.NavigationService.PopLatestArgs<MainNavigationArgs>();
			if (args is null)
				return;

			if (this.BindingContext is not MainViewModel viewModel)
				return;

			int? targetIndex = null;

			if (!string.IsNullOrWhiteSpace(args.TargetTabKey))
			{
				int? index = viewModel.Tabs
					.Select((tab, i) => new { Tab = tab, Index = i })
					.Where(pair => string.Equals(pair.Tab.Key, args.TargetTabKey, StringComparison.OrdinalIgnoreCase))
					.Select(pair => (int?)pair.Index)
					.FirstOrDefault();

				targetIndex = index;
			}

			if (!targetIndex.HasValue && args.TargetTabIndex.HasValue)
			{
				int candidate = args.TargetTabIndex.Value;
				if (candidate >= 0 && candidate < viewModel.Tabs.Count)
					targetIndex = candidate;
			}

			if (targetIndex.HasValue)
				viewModel.SelectedTabIndex = targetIndex.Value;
		}

		private View ResolveView<T>(string viewKey)
			where T : View
		{
			try
			{
				View? Resolved = ServiceHelper.GetService<T>();
				if (Resolved is not null)
					return Resolved;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return this.CreateFallbackView(viewKey);
		}

		private View CreateFallbackView(string viewKey)
		{
			return new ContentView
			{
				Content = new Label
				{
					Text = $"Unable to load {viewKey} view.",
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};
		}

		private void EnsureSelectedTabIndex()
		{
			int MaxIndex = this.ContentSwitcher.Views.Count - 1;
			if (MaxIndex < 0)
			{
				this.ContentSwitcher.SelectedIndex = 0;
				return;
			}

			if (this.ContentSwitcher.SelectedIndex > MaxIndex || this.ContentSwitcher.SelectedIndex < 0)
				this.ContentSwitcher.SelectedIndex = 0;
		}
	}
}
