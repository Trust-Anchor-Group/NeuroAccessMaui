using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// A row in the list of a chat: a message, or the day the messages below it were written.
	/// </summary>
	public abstract class ChatItem : ObservableObject
	{
		/// <summary>
		/// A row in the list of a chat.
		/// </summary>
		/// <param name="Timestamp">When the row's message was written, in local time.</param>
		protected ChatItem(DateTime Timestamp)
		{
			this.Timestamp = Timestamp;
		}

		/// <summary>
		/// When the row's message was written, in local time.
		/// </summary>
		public DateTime Timestamp { get; protected set; }
	}
}
