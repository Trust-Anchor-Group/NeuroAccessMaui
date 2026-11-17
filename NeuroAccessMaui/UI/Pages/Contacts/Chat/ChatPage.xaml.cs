using System.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// A chat with a contact, or notes to yourself.
	/// </summary>
	public partial class ChatPage
	{
		private const double menuMargin = 12;
		private const double menuGap = 8;
		private const double liftedScale = 1.03;

		private readonly ChatViewModel viewModel;
		private int firstVisible;
		private bool? hideSoftInputOnTapped;
		private int menuVersion;

		/// <summary>
		/// Where the row of a pressed message is on the page, if the platform reported it. The menu opens next to it.
		/// </summary>
		private Rect? menuAnchor;

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
			this.viewModel.HideKeyboardRequested += this.ViewModel_HideKeyboardRequested;
			this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;

			this.ContentPageModel = this.viewModel;
			this.InitializeComponent();

			this.MessageList.HandlerChanged += this.MessageList_HandlerChanged;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			// The formatting and send buttons are tapped while writing, so a tap outside the text field keeps the keyboard.
			if (Shell is ContentPage Host && this.hideSoftInputOnTapped is null)
			{
				this.hideSoftInputOnTapped = Host.HideSoftInputOnTapped;
				Host.HideSoftInputOnTapped = false;
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			if (Shell is ContentPage Host && this.hideSoftInputOnTapped is bool Hide)
				Host.HideSoftInputOnTapped = Hide;

			this.hideSoftInputOnTapped = null;

			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			this.viewModel.ScrollToEndRequested -= this.ViewModel_ScrollToEndRequested;
			this.viewModel.OlderRowsAdded -= this.ViewModel_OlderRowsAdded;
			this.viewModel.FocusComposerRequested -= this.ViewModel_FocusComposerRequested;
			this.viewModel.HideKeyboardRequested -= this.ViewModel_HideKeyboardRequested;
			this.viewModel.PropertyChanged -= this.ViewModel_PropertyChanged;
			this.MessageList.HandlerChanged -= this.MessageList_HandlerChanged;
			this.DetachLongPress();

			await base.OnDisposeAsync();
		}

		/// <summary>
		/// The page that hosts all pages, whose keyboard handling the chat changes while it shows.
		/// </summary>
		private static Page? Shell => Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;

		private void MessageList_HandlerChanged(object? Sender, EventArgs e)
		{
			this.AttachLongPress();
		}

		/// <summary>
		/// Lets a message be pressed and held anywhere to show its menu, where the platform needs more than the bubble's
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

		private void ViewModel_HideKeyboardRequested(object? Sender, EventArgs e)
		{
			this.Composer.Unfocus();
			ServiceRef.PlatformSpecific.HideKeyboard();
		}

		private void ViewModel_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ChatViewModel.MenuItem))
			{
				if (this.viewModel.MenuItem is ChatMessageItem Item)
					_ = this.ShowMenuAsync(Item);
				else
					_ = this.HideMenuAsync();
			}
		}

		private void MessageList_Scrolled(object? Sender, ItemsViewScrolledEventArgs e)
		{
			this.firstVisible = e.FirstVisibleItemIndex;
			this.viewModel.OnListScrolled(e.FirstVisibleItemIndex, e.LastVisibleItemIndex);
		}

		private void Composer_Focused(object? Sender, FocusEventArgs e)
		{
			this.viewModel.OnComposerFocusChanged(true);
		}

		private void Composer_Unfocused(object? Sender, FocusEventArgs e)
		{
			this.viewModel.OnComposerFocusChanged(false);
		}

		#region Formatting

		/// <summary>
		/// Applies a button of the formatting bar to the selected text, or at the cursor, and keeps the text field focused.
		/// </summary>
		private void FormatButton_Clicked(object? Sender, EventArgs e)
		{
			if (Sender is not TemplatedButton Button || !Enum.TryParse(Button.CommandParameter as string, out ChatFormat Format))
				return;

			string Text = this.Composer.Text ?? string.Empty;
			ChatFormatting.Selection Result = ChatFormatting.Apply(Format,
				new ChatFormatting.Selection(Text, this.Composer.CursorPosition, this.Composer.SelectionLength));

			this.Composer.Text = Result.Text;

			if (!this.Composer.IsFocused)
				this.Composer.Focus();

			// The new text is set first, as setting it moves the cursor to its end.
			this.Dispatcher.Dispatch(() =>
			{
				this.Composer.CursorPosition = Result.Start;
				this.Composer.SelectionLength = Result.Length;
			});
		}

		#endregion

		#region Message menu

		/// <summary>
		/// Shows the menu of a message: the chat dims, the message is shown again lifted where it is, and the menu opens
		/// below it, or above it when there is no room below.
		/// </summary>
		private async Task ShowMenuAsync(ChatMessageItem Item)
		{
			int Version = ++this.menuVersion;
			Rect? Anchor = this.menuAnchor;
			this.menuAnchor = null;

			try
			{
				View? Row = this.CreateLiftedRow(Item);
				Border? Bubble = Row?.FindByName<Border>("Bubble");

				this.LiftedHost.Content = Row;
				this.MenuLayer.Opacity = 0;
				this.MenuLayer.IsVisible = true;

				// Waits for the lifted message and the menu to get their sizes.
				await Task.Delay(48);
				if (Version != this.menuVersion)
					return;

				this.PlaceMenu(Item, Anchor);

				this.MenuCard.Scale = 0.92;
				this.MenuCard.AnchorX = Item.IsSent ? 1 : 0;
				this.MenuCard.AnchorY = 0;

				Task Fade = this.MenuLayer.FadeToAsync(1, 140, Easing.CubicOut);
				Task Grow = this.MenuCard.ScaleToAsync(1, 180, Easing.CubicOut);
				Task Lift = Bubble is null ? Task.CompletedTask : Bubble.ScaleToAsync(liftedScale, 180, Easing.CubicOut);

				await Task.WhenAll(Fade, Grow, Lift);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task HideMenuAsync()
		{
			int Version = ++this.menuVersion;

			try
			{
				if (this.MenuLayer.IsVisible)
					await this.MenuLayer.FadeToAsync(0, 110, Easing.CubicIn);

				if (Version != this.menuVersion)
					return;

				this.MenuLayer.IsVisible = false;
				this.LiftedHost.Content = null;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Creates the row of a message again, from the list's own template, so it looks exactly like the row in the list.
		/// </summary>
		private View? CreateLiftedRow(ChatMessageItem Item)
		{
			if (this.Resources.TryGetValue("ChatItemTemplates", out object? Value) && Value is DataTemplateSelector Selector &&
				Selector.SelectTemplate(Item, this.MessageList)?.CreateContent() is View Row)
			{
				Row.BindingContext = Item;
				Row.InputTransparent = true;

				if (Row.FindByName<Border>("Bubble") is Border Bubble)
				{
					Bubble.AnchorX = Item.IsSent ? 1 : 0;
					Bubble.Shadow = new Shadow()
					{
						Brush = Colors.Black,
						Offset = new Point(0, 10),
						Radius = 28,
						Opacity = 0.28f
					};
				}

				return Row;
			}

			return null;
		}

		/// <summary>
		/// Places the lifted message where its row is, and the menu next to it, on the side of the message. When there is
		/// no room for the menu below or above the message, the message moves up so both fit.
		/// </summary>
		private void PlaceMenu(ChatMessageItem Item, Rect? Anchor)
		{
			double Width = this.MenuLayer.Width;
			double Height = this.MenuLayer.Height;
			double RowHeight = Math.Max(this.LiftedHost.Height, 0);
			double MenuHeight = Math.Max(this.MenuCard.Height, 0);
			double MenuWidth = this.MenuCard.Width > 0 ? this.MenuCard.Width : 250;
			double BubbleTop = Item.Spacing.Top;

			double RowTop = Anchor?.Y ?? (Height - RowHeight - menuGap - MenuHeight) / 2;
			double MenuTop = RowTop + RowHeight + menuGap;

			if (MenuTop + MenuHeight > Height - menuMargin)
			{
				double Above = RowTop + BubbleTop - menuGap - MenuHeight;

				if (Above >= menuMargin)
					MenuTop = Above;
				else
				{
					MenuTop = Height - menuMargin - MenuHeight;
					RowTop = Math.Max(menuMargin - BubbleTop, MenuTop - menuGap - RowHeight);
				}
			}

			double MenuLeft = Item.IsSent ? Width - menuMargin - MenuWidth : menuMargin;

			AbsoluteLayout.SetLayoutBounds(this.LiftedHost, new Rect(0, RowTop, 1, AbsoluteLayout.AutoSize));
			AbsoluteLayout.SetLayoutBounds(this.MenuCard, new Rect(MenuLeft, Math.Max(menuMargin, MenuTop), MenuWidth, AbsoluteLayout.AutoSize));
		}

		#endregion
	}
}
