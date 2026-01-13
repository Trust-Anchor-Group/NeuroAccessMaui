using System;
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
			ChatNavigationArgs args = ServiceRef.UiService.PopLatestArgs<ChatNavigationArgs>() ?? new ChatNavigationArgs();
			this.ContentPageModel = new ChatSessionViewModel(args);
			this.InitializeComponent();
		}

		private async void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
		{
			if (this.ContentPageModel is ChatSessionViewModel viewModel)
				await viewModel.LoadOlderMessagesAsync();
		}
	}
}
