namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.Controls
{
	/// <summary>
	/// Border containing sent messages.
	/// </summary>
	public abstract class MessageFrame : Border
	{
		/// <summary>
		/// Abstract base class for message frames.
		/// </summary>
		public MessageFrame()
			: base()
		{
			this.Messages = [];
			this.Content = this.Messages;
		}

		/// <summary>
		/// Type of message in frame
		/// </summary>
		public abstract MessageType MessageType { get; }

		/// <summary>
		/// Messages in frame.
		/// </summary>
		public VerticalStackLayout Messages { get; }

		/// <summary>
		/// Creates a message frame for a given message.
		/// </summary>
		/// <param name="Message">Chat message.</param>
		/// <returns>New message frame.</returns>
		/// <exception cref="InvalidOperationException">If Message Type was not supported.</exception>
		public static MessageFrame Create(ChatMessage Message)
		{
			return Message.MessageType switch
			{
				MessageType.Sent => new SendFrame(),
				MessageType.Received => new ReceiveFrame(),
				_ => throw new InvalidOperationException("Unhandled message type: " + Message.MessageType.ToString()),
			};
		}

		private static IView GetView(ChatMessage Message)
		{
			if (Message.ParsedXaml is not IView View)
			{
				View = new Label()
				{
					Text = Message.PlainText
				};
			}

			return View;
		}

		/// <summary>
		/// Adds a message to the frame.
		/// </summary>
		/// <param name="Message">Message</param>
		public IView AddLast(ChatMessage Message)
		{
			IView View = GetView(Message);
			this.Messages.Add(View);
			return View;
		}

		/// <summary>
		/// Adds a message to the frame as the first message.
		/// </summary>
		/// <param name="Message">Message</param>
		public IView AddFirst(ChatMessage Message)
		{
			IView View = GetView(Message);
			this.Messages.Insert(0, View);
			return View;
		}

	}
}
