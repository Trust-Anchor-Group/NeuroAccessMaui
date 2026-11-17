namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// Picks the template of a chat row: a sent or received message, or a day.
	/// </summary>
	public sealed class ChatItemTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template of messages the user sent.
		/// </summary>
		public DataTemplate? SentTemplate { get; set; }

		/// <summary>
		/// Template of messages the user received.
		/// </summary>
		public DataTemplate? ReceivedTemplate { get; set; }

		/// <summary>
		/// Template of day headers.
		/// </summary>
		public DataTemplate? DayTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate? OnSelectTemplate(object Item, BindableObject Container)
		{
			return Item switch
			{
				ChatMessageItem Message when Message.IsSent => this.SentTemplate,
				ChatMessageItem => this.ReceivedTemplate,
				_ => this.DayTemplate
			};
		}
	}
}
