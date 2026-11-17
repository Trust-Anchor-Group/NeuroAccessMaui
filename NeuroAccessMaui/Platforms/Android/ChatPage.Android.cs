using Android.Views;
using AndroidX.RecyclerView.Widget;
using AView = Android.Views.View;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// Android part of <see cref="ChatPage"/>: a message is pressed and held anywhere to show its actions, also on a link,
	/// card or image in it.
	/// </summary>
	/// <remarks>
	/// Links, cards and images take the touches that start on them, so a touch behavior on the bubble never sees those. The
	/// list sees every touch before the rows do, so it watches for the hold there. When a message is held, the list takes
	/// the rest of the touch, and the link, card or image under the finger is not opened.
	/// </remarks>
	public partial class ChatPage
	{
		private RecyclerView? longPressList;
		private MessageLongPressListener? longPressListener;

		partial void AttachLongPress()
		{
			this.DetachLongPress();

			if (this.MessageList.Handler?.PlatformView is not RecyclerView List)
				return;

			MessageLongPressListener Listener = new(List, this.OnMessageLongPressed);
			List.AddOnItemTouchListener(Listener);

			this.longPressList = List;
			this.longPressListener = Listener;
		}

		partial void DetachLongPress()
		{
			if (this.longPressList is not null && this.longPressListener is not null)
				this.longPressList.RemoveOnItemTouchListener(this.longPressListener);

			this.longPressListener?.Dispose();
			this.longPressListener = null;
			this.longPressList = null;
		}

		/// <summary>
		/// Shows the actions of the message in a row of the list.
		/// </summary>
		/// <param name="Position">Position of the row.</param>
		/// <returns>If the row is a message.</returns>
		private bool OnMessageLongPressed(int Position)
		{
			ChatViewModel ViewModel = this.viewModel;

			if (Position < 0 || Position >= ViewModel.Items.Count || ViewModel.Items[Position] is not ChatMessageItem Item)
				return false;

			if (ViewModel.ActionsCommand.CanExecute(Item))
				ViewModel.ActionsCommand.Execute(Item);

			return true;
		}

		/// <summary>
		/// Watches the touches of the message list for a press that is held, without taking them from the rows until then.
		/// </summary>
		private sealed class MessageLongPressListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
		{
			private readonly RecyclerView list;
			private readonly Func<int, bool> onLongPress;
			private readonly GestureDetector detector;
			private bool holding;
			private bool released;

			/// <summary>
			/// Watches the touches of a message list.
			/// </summary>
			/// <param name="List">The list.</param>
			/// <param name="OnLongPress">Called with the position of a row that is held. Returns if the row is a message.</param>
			public MessageLongPressListener(RecyclerView List, Func<int, bool> OnLongPress)
			{
				this.list = List;
				this.onLongPress = OnLongPress;
				this.detector = new GestureDetector(List.Context, new HoldListener(this));
			}

			/// <inheritdoc/>
			public bool OnInterceptTouchEvent(RecyclerView Rv, MotionEvent E)
			{
				if (E.ActionMasked == MotionEventActions.Down)
				{
					this.holding = false;
					this.released = false;
				}

				this.detector.OnTouchEvent(E);

				// After a hold, the list takes the rest of the touch, so the row under the finger gets a cancel, not a tap.
				return this.holding;
			}

			/// <inheritdoc/>
			public void OnTouchEvent(RecyclerView Rv, MotionEvent E)
			{
			}

			/// <inheritdoc/>
			public void OnRequestDisallowInterceptTouchEvent(bool DisallowIntercept)
			{
				// A row that scrolls by itself, such as a wide table, has taken the touch. Holding it is not a long press.
				if (DisallowIntercept)
					this.released = true;
			}

			private void Held(MotionEvent E)
			{
				if (this.released)
					return;

				AView? Row = this.list.FindChildViewUnder(E.GetX(), E.GetY());
				if (Row is null)
					return;

				int Position = this.list.GetChildAdapterPosition(Row);
				if (Position == RecyclerView.NoPosition || !this.onLongPress(Position))
					return;

				this.holding = true;
				this.list.PerformHapticFeedback(FeedbackConstants.LongPress);
			}

			/// <summary>
			/// Reports a press that is held.
			/// </summary>
			/// <param name="Owner">The listener that gets the report.</param>
			private sealed class HoldListener(MessageLongPressListener Owner) : GestureDetector.SimpleOnGestureListener
			{
				/// <inheritdoc/>
				public override void OnLongPress(MotionEvent E)
				{
					Owner.Held(E);
				}
			}
		}
	}
}
