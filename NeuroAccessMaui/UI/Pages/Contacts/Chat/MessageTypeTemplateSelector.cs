namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// Data Template Selector, based on Message Type.
	/// </summary>
	public class MessageTypeTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// An empty transparent bubble, used to fix an issue on iOS
		/// </summary>
		public DataTemplate? EmptyTemplate { get; set; }

		/// <summary>
		/// Template to use for sent messages.
		/// </summary>
		public DataTemplate? SentTemplate { get; set; }

		/// <summary>
		/// Template to use for received messages.
		/// </summary>
		public DataTemplate? ReceivedTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate? DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
		{
			if (item is ChatMessage Message)
			{
				return Message.MessageType switch
				{
					MessageType.Sent => this.SentTemplate ?? this.DefaultTemplate,
					MessageType.Received => this.ReceivedTemplate ?? this.DefaultTemplate,
					MessageType.Empty => this.EmptyTemplate ?? this.DefaultTemplate,
					_ => this.DefaultTemplate,
				};
			}

			return this.DefaultTemplate;
		}
	}
}
