using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Main
{
	/// <summary>
	/// Provides the tab definitions and selection state for the main view-switcher surface.
	/// </summary>
	public partial class MainViewModel : BaseViewModel
	{
		private readonly ObservableCollection<TabDefinition> tabs;
		private readonly TabDefinition chatsTab;

		/// <summary>
		/// Initializes a new instance of the <see cref="MainViewModel"/> class.
		/// </summary>
		public MainViewModel()
		{
			this.chatsTab = this.CreateTab("chats", "message.svg", ServiceRef.Localizer[nameof(AppResources.ChatsTitle)]);

			this.tabs = new ObservableCollection<TabDefinition>
			{
				this.CreateTab("home", "home.svg", ServiceRef.Localizer[nameof(AppResources.Home)]),
				this.CreateTab("wallet", "wallet.svg", ServiceRef.Localizer[nameof(AppResources.Wallet)], isProminent: true),
				this.chatsTab,
				this.CreateTab("apps", "apps.svg", ServiceRef.Localizer[nameof(AppResources.Apps)])
			};

			this.Tabs = new ReadOnlyObservableCollection<TabDefinition>(this.tabs);
			this.SelectedTabIndex = 0;

			ConversationStore.Changed += this.ConversationStore_Changed;
			_ = this.UpdateUnreadBadgeAsync();
		}

		/// <summary>
		/// Gets the collection of tabs displayed in the navigation host.
		/// </summary>
		public ReadOnlyObservableCollection<TabDefinition> Tabs { get; }

		/// <summary>
		/// Gets or sets the selected tab index.
		/// </summary>
		[ObservableProperty]
		private int selectedTabIndex;

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			ConversationStore.Changed -= this.ConversationStore_Changed;
			return base.OnDisposeAsync();
		}

		private void ConversationStore_Changed(object? Sender, EventArgs e)
		{
			_ = this.UpdateUnreadBadgeAsync();
		}

		/// <summary>
		/// Shows the number of unread messages, in chats that are not muted, on the Chats tab.
		/// </summary>
		private async Task UpdateUnreadBadgeAsync()
		{
			try
			{
				int Unread = await ConversationStore.GetUnreadTotalAsync();
				MainThread.BeginInvokeOnMainThread(() => this.chatsTab.BadgeCount = Unread > 0 ? Unread : null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private TabDefinition CreateTab(string key, string icon, string title, bool isProminent = false)
		{
			return new TabDefinition
			{
				Key = key,
				Icon = icon,
				Title = title,
				IsProminent = isProminent
			};
		}
	}
}
