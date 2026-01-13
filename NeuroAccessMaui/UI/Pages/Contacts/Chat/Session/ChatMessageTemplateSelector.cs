using System;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services.Chat.Models;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.Session
{
	/// <summary>
	/// Selects the appropriate message template based on direction.
	/// </summary>
	public sealed class ChatMessageTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template applied to outgoing messages.
		/// </summary>
		public DataTemplate? SentMessageTemplate { get; set; }

		/// <summary>
		/// Template applied to incoming messages.
		/// </summary>
		public DataTemplate? ReceivedMessageTemplate { get; set; }

		/// <summary>
		/// Template applied to system messages.
		/// </summary>
		public DataTemplate? SystemMessageTemplate { get; set; }

		/// <inheritdoc />
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is not ChatMessageItemViewModel message)
				return this.ReceivedMessageTemplate ?? new DataTemplate();

			return message.Direction switch
			{
				ChatMessageDirection.Outgoing => this.SentMessageTemplate ?? this.ReceivedMessageTemplate ?? new DataTemplate(),
				ChatMessageDirection.System => this.SystemMessageTemplate ?? this.ReceivedMessageTemplate ?? new DataTemplate(),
				_ => this.ReceivedMessageTemplate ?? new DataTemplate(),
			};
		}
	}
}
