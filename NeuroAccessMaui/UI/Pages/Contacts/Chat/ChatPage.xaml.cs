using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// A chat with a contact, or notes to yourself.
	/// </summary>
	public partial class ChatPage
	{
		private readonly ChatViewModel viewModel;

		/// <inheritdoc/>
		public override string? UniqueId
		{
			get => this.viewModel.UniqueId;
			set => this.viewModel.UniqueId = value;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ChatPage"/> class.
		/// </summary>
		public ChatPage()
		{
			this.viewModel = new ChatViewModel(ServiceRef.NavigationService.PopLatestArgs<ChatNavigationArgs>());
			this.viewModel.ScrollToEndRequested += this.ViewModel_ScrollToEndRequested;
			this.viewModel.OlderRowsAdded += this.ViewModel_OlderRowsAdded;
			this.viewModel.FocusComposerRequested += this.ViewModel_FocusComposerRequested;

			this.ContentPageModel = this.viewModel;
			this.InitializeComponent();

			this.MessageList.HandlerChanged += this.MessageList_HandlerChanged;
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			this.viewModel.ScrollToEndRequested -= this.ViewModel_ScrollToEndRequested;
			this.viewModel.OlderRowsAdded -= this.ViewModel_OlderRowsAdded;
			this.viewModel.FocusComposerRequested -= this.ViewModel_FocusComposerRequested;
			this.MessageList.HandlerChanged -= this.MessageList_HandlerChanged;
			this.DetachLongPress();

			await base.OnDisposeAsync();
		}

		private void MessageList_HandlerChanged(object? Sender, EventArgs e)
		{
			this.AttachLongPress();
		}

		/// <summary>
		/// Lets a message be pressed and held anywhere to show its actions, where the platform needs more than the bubble's
		/// touch behavior for that.
		/// </summary>
		partial void AttachLongPress();

		/// <summary>
		/// Stops what <see cref="AttachLongPress"/> started.
		/// </summary>
		partial void DetachLongPress();

		private void ViewModel_ScrollToEndRequested(object? Sender, bool Animate)
		{
			// The list lays out new rows first, so the scrolling waits a moment.
			this.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(Animate ? 50 : 100), () =>
			{
				int Count = this.viewModel.Items.Count;
				if (Count > 0)
					this.MessageList.ScrollTo(Count - 1, -1, ScrollToPosition.End, Animate);
			});
		}

		private void ViewModel_OlderRowsAdded(object? Sender, int Count)
		{
			// Keeps the messages that were on screen in place, above the older ones that were added.
			int Index = this.firstVisible + Count;

			this.Dispatcher.Dispatch(() =>
			{
				if (Index >= 0 && Index < this.viewModel.Items.Count)
					this.MessageList.ScrollTo(Index, -1, ScrollToPosition.Start, false);
			});
		}

		private void ViewModel_FocusComposerRequested(object? Sender, EventArgs e)
		{
			this.Dispatcher.Dispatch(() => this.Composer.Focus());
		}

		private int firstVisible;

		private void MessageList_Scrolled(object? Sender, ItemsViewScrolledEventArgs e)
		{
			this.firstVisible = e.FirstVisibleItemIndex;
			this.viewModel.OnListScrolled(e.FirstVisibleItemIndex, e.LastVisibleItemIndex);
		}
	}
}
