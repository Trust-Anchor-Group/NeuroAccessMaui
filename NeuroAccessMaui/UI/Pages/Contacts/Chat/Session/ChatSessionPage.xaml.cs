using System;
using System.Collections;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.Session
{
	/// <summary>
	/// Modern chat session page using the new chat stack.
	/// </summary>
	public partial class ChatSessionPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatSessionPage"/> class.
		/// </summary>
		public ChatSessionPage()
		{
			ChatNavigationArgs args = ServiceRef.NavigationService.PopLatestArgs<ChatNavigationArgs>() ?? new ChatNavigationArgs();
			this.ContentPageModel = new ChatSessionViewModel(args);
			this.InitializeComponent();
		}

		private async void OnRemainingItemsThresholdReached(object? Sender, EventArgs Args)
		{
			if (this.ContentPageModel is ChatSessionViewModel ViewModel)
				await ViewModel.LoadOlderMessagesAsync();
		}

		private void OnItemsViewScrolled(object? Sender, ItemsViewScrolledEventArgs Args)
		{
			if (this.ContentPageModel is not ChatSessionViewModel ViewModel)
				return;

			if (Sender is not ItemsView ItemsView || ItemsView.ItemsSource is not IList Items)
				return;

			int FirstIndex = Math.Max(Args.FirstVisibleItemIndex, 0);
			int LastIndex = Math.Min(Args.LastVisibleItemIndex, Items.Count - 1);

			for (int Index = FirstIndex; Index <= LastIndex; Index++)
			{
				if (Items[Index] is ChatMessageItemViewModel Item)
					_ = ViewModel.HandleMessageAppearingAsync(Item);
			}
		}
	}
}


